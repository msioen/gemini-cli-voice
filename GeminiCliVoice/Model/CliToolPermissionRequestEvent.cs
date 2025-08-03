namespace GeminiCliVoice.Model;

public class CliToolPermissionRequestEvent : CliEvent
{
    public string Type { get; set; }
    public string Title { get; set; }
    
    public override int Prepare()
    {
        return 1;
    }

    public override Task HandleAsync(KokoroPlayer ttsPlayer, SoundPlayer soundPlayer, CancellationToken cancellationToken)
    {
        // TODO - auto-approve tool execution (possibly depending on config)
        // v1: simulate yolo modo
        // future: use voice prompt
        return Task.CompletedTask;
    }
}