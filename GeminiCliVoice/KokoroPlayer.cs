using KokoroSharp;
using KokoroSharp.Core;
using KokoroSharp.Processing;

namespace GeminiCliVoice;

public class KokoroPlayer
{
    private KokoroTTS? _tts;
    private Dictionary<string, KokoroVoice> _voices = new  Dictionary<string, KokoroVoice>();

    private Task? _initTask;

    public KokoroPlayer()
    {
        _initTask = InitAsync();
    }

    public async Task PlayAsync(string text, string voice = "af_heart", CancellationToken cancellationToken = default)
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

            var config = new KokoroTTSPipelineConfig()
            {
                Speed = 1.2f,
                SecondsOfPauseBetweenProperSegments = new PauseAfterSegmentStrategy(0.1f, 0.2f, 0.2f, 0.2f, 0.2f, 0.2f),
            };

            if (!_voices.TryGetValue(voice, out var kokoroVoice))
            {
                kokoroVoice = KokoroVoiceManager.GetVoice(voice);
                _voices.Add(voice, kokoroVoice);
            }
            var handle = _tts.SpeakFast(text, kokoroVoice, config);
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
        _voices.Add("af_heart", KokoroVoiceManager.GetVoice("af_heart"));
    }
}