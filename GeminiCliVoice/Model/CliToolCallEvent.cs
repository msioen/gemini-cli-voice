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
        return 1;
    }
    
    public override Task HandleAsync(KokoroPlayer ttsPlayer, SoundPlayer soundPlayer, CancellationToken cancellationToken)
    {
        Console.WriteLine($"starting to play (tool): Tool call: {FunctionName}");
        
        return ttsPlayer.PlayAsync($"Tool call: {FunctionName}", cancellationToken);
    }
}