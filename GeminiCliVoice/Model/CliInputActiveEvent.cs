namespace GeminiCliVoice.Model;

public class CliInputActiveEvent : CliEvent
{
    public bool Active { get; set; }
    
    public override int Prepare()
    {
        return PriorityDefault;
    }
    
    public override Task HandleAsync(Context context, CancellationToken cancellationToken)
    {
        if (context.IsReplayMode || !Active)
        {
            return Task.CompletedTask;
        }
        
        return HandleInputLoopAsync(context, cancellationToken);
    }
}