namespace GeminiCliVoice.Model;

public class CliUserPromptEvent : CliEvent
{
    public string PromptText { get; set; }

    public override int Prepare()
    {
        return PriorityDefault;
    }

    public override Task HandleAsync(Context context, CancellationToken cancellationToken)
    {
        return context.SoundPlayer.PlaySoundAsync("mixkit-retro-game-notification-212.wav", cancellationToken);
    }
}