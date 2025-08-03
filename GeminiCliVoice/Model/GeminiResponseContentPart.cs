using System.Text.Json.Serialization;

namespace GeminiCliVoice.Model;

public class GeminiResponseContentPart
{
    [JsonPropertyName("thought")]
    public bool? Thought { get; set; }
    
    [JsonPropertyName("text")]
    public string Text { get; set; }
}