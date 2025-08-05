namespace GeminiCliVoice.Model;

public class CliUserPromptEvent : CliEvent
{
    public string PromptText { get; set; }

    public override int Prepare()
    {
        return PriorityDefault;
    }

    public override async Task HandleAsync(Context context, CancellationToken cancellationToken)
    {
        // in replay mode, we'll play back the prompt text
        // note - this does mess up the timeline somewhat since we're playing back the prompt text when it was fully received in previous events
        if (context.IsReplayMode && !string.IsNullOrWhiteSpace(PromptText))
        {
            await context.SoundPlayer.PlaySoundAsync("new-notification-010-352755.wav", cancellationToken);
            await context.KokoroPlayer.PlayAsync(PromptText, "am_adam", cancellationToken);
            await context.SoundPlayer.PlaySoundAsync("notification-alert-269289.wav", cancellationToken);
        }
    }
}