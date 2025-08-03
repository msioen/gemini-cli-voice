using System.Text.Json.Serialization;

namespace GeminiCliVoice.Model;

public class GeminiResponseCandidate
{
    [JsonPropertyName("content")]
    public GeminiResponseContent Content { get; set; }
}