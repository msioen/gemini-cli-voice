namespace GeminiCliVoice.Model;

public abstract class CliEvent
{
    protected const int PriorityCanIgnore = 0;
    protected const int PriorityDefault = 2;
    
    public string EventName { get; set; }
    public DateTime EventTimestamp { get; set; }
    public string Body{ get; set; }
    
    public abstract int Prepare();
    
    public abstract Task HandleAsync(Context context, CancellationToken cancellationToken);
    
    protected async Task HandleInputLoopAsync(Context context, CancellationToken cancellationToken)
    {
        var attempts = 2;
        var input = string.Empty;
        while(attempts > 0 && string.IsNullOrWhiteSpace(input))
        {
            // TODO - play a sound indicating input is expected
            input = await context.WhisperManager.GetTranscribedMicrophoneInputAsync(4000, cancellationToken);
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