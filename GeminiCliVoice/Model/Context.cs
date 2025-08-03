namespace GeminiCliVoice.Model;

public class Context
{
    public bool IsReplayMode { get; set; }
    
    public KokoroPlayer KokoroPlayer { get; set; }
    public SoundPlayer SoundPlayer { get; set; }
    public WhisperManager WhisperManager { get; set; }
    public GeminiCliManager GeminiCliManager { get; set; }
}