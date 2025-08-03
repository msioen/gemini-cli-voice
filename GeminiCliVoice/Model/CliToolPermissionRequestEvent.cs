namespace GeminiCliVoice.Model;

public class CliToolPermissionRequestEvent : CliEvent
{
    public string Type { get; set; }
    public string Title { get; set; }
    
    public override int Prepare()
    {
        return PriorityDefault;
    }

    public override async Task HandleAsync(Context context, CancellationToken cancellationToken)
    {
        // auto-approve tool execution (possibly depending on config)
        // v1: simulate yolo modo
        // future: use voice prompt
        if (!context.IsReplayMode)
        {
            var ttsTask = context.KokoroPlayer.PlayAsync("Auto approving tool execution: " + Title, cancellationToken);
            var inputTask = context.GeminiCliManager.InputAsync(string.Empty, cancellationToken);
            await Task.WhenAll(ttsTask, inputTask);
        }
        else
        {
            await context.KokoroPlayer.PlayAsync("Replay - auto approving tool execution: " + Title, cancellationToken);
        }
    }
}