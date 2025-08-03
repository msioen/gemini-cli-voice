using System.Text.Json.Serialization;

namespace GeminiCliVoice.Model;

public class GeminiResponseContent
{
    [JsonPropertyName("parts")]
    public List<GeminiResponseContentPart> Parts { get; set; }
}