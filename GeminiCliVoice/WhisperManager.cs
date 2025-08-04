using System.Diagnostics;

namespace GeminiCliVoice;

public class WhisperManager
{
    private readonly string _rootPath = "/Users/michielsioen/Documents/_code/whisper.cpp";
    
    public WhisperManager()
    {
        
    }
    
    public async Task<string> GetTranscribedMicrophoneInputAsync(int stopSilenceDurationInMs, 
        CancellationToken cancellationToken)
    {
        var outputFile = $"whisper/{DateTime.Now:yyyyMMddHHmmssfff}.txt";
        var activationFile = "whisper/act.bin";

        if (!Directory.Exists("whisper"))
        {
            Directory.CreateDirectory("whisper");
        }
        
        if (File.Exists(outputFile))
        {
            File.Delete(outputFile);
        }
        
        try
        {
            var process = new Process();
            var startInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(_rootPath, "build/bin/whisper-command"),
                Arguments = $"-m {Path.Combine(_rootPath, "models/ggml-base.en.bin")} -t 8 -ac 768 --file {outputFile} -sms {stopSilenceDurationInMs} --file-activation {activationFile}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            process.StartInfo = startInfo;
            
            // note - both outputs actually can be useful, currently dismissing them since they otherwise end up
            // in the same console output as our main program
            process.OutputDataReceived += (sender, args) => { };
            process.ErrorDataReceived += (sender, args) => { };
            
            var result = process.Start();
            if (!result)
            {
                Console.WriteLine("Failed to start whisper-command process.");
                return string.Empty;
            }
            
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (Exception e)
        {
            Console.WriteLine("WhisperManager GetTranscribedMicrophoneInputAsync Error: " + e.Message);
        }

        if (File.Exists(outputFile))
        {
            var output = await File.ReadAllTextAsync(outputFile, cancellationToken);
            output = output.Trim();
            if (output.Length == 0 || output.All(char.IsPunctuation))
            {
                return string.Empty;
            }
            
            return output;
        }

        return string.Empty;
    }
}