using System.Text.Json;

namespace GeminiCliVoice.Model;

public class CliApiResponseEvent : CliEvent
{
    private string _parsedResponse = string.Empty;
    
    public int StatusCode { get; set; }
    public int DurationMs { get; set; }
    public string Error { get; set; }
    public string ResponseText { get; set; }
    
    public override int Prepare()
    {
        // TODO - different prio / contents based on 'thought' vs 'text' => maybe even skip thought alltogether depending on settings
        // => for now skipping thoughts
        var response = JsonSerializer.Deserialize<List<GeminiResponse>>(ResponseText);
        var parts = response?
            .SelectMany(r => r.Candidates)
            .SelectMany(x => 
                x.Content.Parts.Where(y => 
                    !string.IsNullOrWhiteSpace(y.Text) && y.Thought != true))
            .Select(x => x.Text)
            .ToList();
        
        if (parts != null && parts.Any())
        {
            _parsedResponse = string.Join(" ", parts);
            return 5;
        }
        
        return 0;
    }
    
    public override Task HandleAsync(KokoroPlayer ttsPlayer, SoundPlayer soundPlayer, CancellationToken cancellationToken)
    {
        Console.WriteLine("starting to play (response): " + _parsedResponse);
        
        return ttsPlayer.PlayAsync(_parsedResponse, cancellationToken);
    }
}