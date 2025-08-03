namespace GeminiCliVoice.Model;

public class CliInputActiveEvent : CliEvent
{
    public bool Active { get; set; }
    
    public override int Prepare()
    {
        return 1;
    }
    
    public override Task HandleAsync(KokoroPlayer ttsPlayer, SoundPlayer soundPlayer, CancellationToken cancellationToken)
    {
        // TODO - if active ensure we're listening for input
        return Task.CompletedTask;
    }
}