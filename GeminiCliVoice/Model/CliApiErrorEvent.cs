namespace GeminiCliVoice.Model;

public class CliApiErrorEvent : CliEvent
{
    public string Error { get; set; }
    public string ErrorType { get; set; }
    public string ErrorMessage { get; set; }
    
    public override int Prepare()
    {
        return 5;
    }
    
    public override Task HandleAsync(KokoroPlayer ttsPlayer, SoundPlayer soundPlayer, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}