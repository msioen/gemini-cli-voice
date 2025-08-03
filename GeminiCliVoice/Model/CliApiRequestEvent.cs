namespace GeminiCliVoice.Model;

public class CliApiRequestEvent : CliEvent
{
    public string RequestText { get; set; }

    public override int Prepare()
    {
        return PriorityCanIgnore;
    }
    
    public override Task HandleAsync(Context context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}