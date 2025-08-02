using LibVLCSharp.Shared;

namespace GeminiCliVoice;

public class SoundPlayer
{
    public async Task PlaySoundAsync(string fileName, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var ctr = cancellationToken.Register(() => tcs.TrySetCanceled());
        
        using var libvlc = new LibVLC(enableDebugLogs: true);
        using var media = new Media(libvlc, new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sounds", fileName)));
        using var mediaplayer = new MediaPlayer(media);
            
        mediaplayer.EncounteredError += (sender, args) =>
        {
            tcs.SetException(new Exception(args.ToString()));
        };
        mediaplayer.EndReached += (sender, args) =>
        {
            tcs.SetResult();
        };
        mediaplayer.Play();
        await tcs.Task;
    }
}