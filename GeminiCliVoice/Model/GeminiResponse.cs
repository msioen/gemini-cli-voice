using System.Text.Json.Serialization;

namespace GeminiCliVoice.Model;

public class GeminiResponse
{
    [JsonPropertyName("candidates")]
    public List<GeminiResponseCandidate> Candidates { get; set; }
}