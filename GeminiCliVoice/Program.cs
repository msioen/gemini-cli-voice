using GeminiCliVoice;

// var soundPlayer = new SoundPlayer();
// await soundPlayer.PlaySoundAsync("mixkit-correct-answer-tone-2870.wav", CancellationToken.None);
//
// var kokoroPlayer = new KokoroPlayer();
// await kokoroPlayer.PlayAsync("Hello, this is a test of the Kokoro voice synthesis system.", CancellationToken.None);
//
// var whisperManager = new WhisperManager();
// var text = await whisperManager.GetTranscribedMicrophoneInputAsync(4000, CancellationToken.None);
// Console.WriteLine("transcribed data:");
// Console.WriteLine(text);
// Console.WriteLine();

var replayer = new GeminiCliOtelReplayer();
await replayer.ReplayFileAsync("/Users/michielsioen/Desktop/20250727-vibed-forecast-adaptation.log");