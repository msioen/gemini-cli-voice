namespace GeminiCliVoice.Model;

public abstract class CliEvent
{
    private DateTime? _lastInputTimestamp;
    
    protected const int PriorityCanIgnore = 0;
    protected const int PriorityDefault = 2;
    
    public string EventName { get; set; }
    public DateTime EventTimestamp { get; set; }
    public string Body{ get; set; }
    
    public abstract int Prepare();
    
    public abstract Task HandleAsync(Context context, CancellationToken cancellationToken);
    
    protected async Task HandleInputLoopAsync(Context context, CancellationToken cancellationToken)
    {
        // add small delay if it's the first input request (for the session / for a while)
        if (_lastInputTimestamp == null || _lastInputTimestamp.Value.AddMinutes(30) < DateTime.UtcNow)
        {
            await Task.Delay(1000, cancellationToken);
        }
        
        _lastInputTimestamp = DateTime.UtcNow;
        
        var attempts = 2;
        var input = string.Empty;
        while(attempts > 0 && string.IsNullOrWhiteSpace(input))
        {
            // play sound indicating awaiting input
            var playSoundTask = context.SoundPlayer.PlaySoundAsync("mixkit-correct-answer-tone-2870.wav", cancellationToken);
            var inputTask = context.WhisperManager.GetTranscribedMicrophoneInputAsync(4000, cancellationToken);
            await Task.WhenAll(playSoundTask, inputTask);

            input = await inputTask;
            if (string.IsNullOrWhiteSpace(input))
            {
                attempts--;
                var feedback = attempts > 0 ? "I didn't hear anything, please try again." : "No input received, going to sleep.";
                await context.KokoroPlayer.PlayAsync(feedback, cancellationToken);
            }
        }
        
        if (!string.IsNullOrWhiteSpace(input))
        {
            // TODO - should we playback the received input?
            await context.GeminiCliManager.InputAsync(input, cancellationToken);
        }
    }
}