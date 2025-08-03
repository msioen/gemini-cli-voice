namespace GeminiCliVoice.Model;

public abstract class CliEvent
{
    public string EventName { get; set; }
    public DateTime EventTimestamp { get; set; }
    public string Body{ get; set; }
    
    public abstract int Prepare();
    public abstract Task HandleAsync(KokoroPlayer ttsPlayer, SoundPlayer soundPlayer, CancellationToken cancellationToken);
}