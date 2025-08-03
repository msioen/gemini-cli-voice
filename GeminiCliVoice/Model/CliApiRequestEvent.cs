namespace GeminiCliVoice.Model;

public class CliApiRequestEvent : CliEvent
{
    public string RequestText { get; set; }

    public override int Prepare()
    {
        return 0;
    }
    
    public override Task HandleAsync(KokoroPlayer ttsPlayer, SoundPlayer soundPlayer, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}