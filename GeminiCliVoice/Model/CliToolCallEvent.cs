namespace GeminiCliVoice.Model;

public class CliToolCallEvent : CliEvent
{
    public string FunctionName{ get; set; }
    public string FunctionArguments { get; set; }
    public bool Success { get; set; }
    public string Decision { get; set; }
    public string Error { get; set; }
    public string ErrorType { get; set; }
    
    public override int Prepare()
    {
        return PriorityDefault;
    }
    
    public override Task HandleAsync(Context context, CancellationToken cancellationToken)
    {
        return context.KokoroPlayer.PlayAsync($"Tool call: {FunctionName}", cancellationToken);
    }
}