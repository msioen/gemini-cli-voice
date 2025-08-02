using KokoroSharp;
using KokoroSharp.Core;

namespace GeminiCliVoice;

public class KokoroPlayer
{
    private KokoroTTS? _tts;
    private KokoroVoice? _voice;

    private Task? _initTask;

    public KokoroPlayer()
    {
        _initTask = InitAsync();
    }

    public async Task PlayAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_initTask != null && !_initTask.IsCompleted)
            {
                await _initTask;
                _initTask = null;
            }
            
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await using var ctr = cancellationToken.Register(() => tcs.TrySetCanceled());

            var handle = _tts.SpeakFast(text, _voice);
            handle.OnSpeechCanceled = _ => tcs.TrySetCanceled();
            handle.OnSpeechCompleted = _ => tcs.SetResult();
            await tcs.Task;
        }
        catch(Exception e)
        {
            Console.WriteLine("KokoroPlayer PlayAsync Error: " + e.Message);
        }
    }
    
    private async Task InitAsync()
    {
        _tts = await KokoroTTS.LoadModelAsync(); 
        _voice = KokoroVoiceManager.GetVoice("af_sarah");
    }
}