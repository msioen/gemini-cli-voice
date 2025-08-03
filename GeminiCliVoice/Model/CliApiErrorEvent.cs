namespace GeminiCliVoice.Model;

public class CliApiErrorEvent : CliEvent
{
    public string Error { get; set; }
    public string ErrorType { get; set; }
    public string ErrorMessage { get; set; }
    
    public override int Prepare()
    {
        return PriorityDefault;
    }
    
    public override Task HandleAsync(Context context, CancellationToken cancellationToken)
    {
        return context.KokoroPlayer.PlayAsync(Error, cancellationToken);
    }
}