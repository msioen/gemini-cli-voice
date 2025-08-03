namespace GeminiCliVoice.Model;

public class CliUserPromptEvent : CliEvent
{
    public string PromptText { get; set; }

    public override int Prepare()
    {
        return 1;
    }

    public override Task HandleAsync(KokoroPlayer ttsPlayer, SoundPlayer soundPlayer, CancellationToken cancellationToken)
    {
        return soundPlayer.PlaySoundAsync("mixkit-correct-answer-tone-2870.wav", cancellationToken);
    }
}