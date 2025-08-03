using System.Diagnostics;

namespace GeminiCliVoice;

// note - current implementation uses some (badly written) AppleScript scripts
// => ideally we could instead fully manage the process ourselves here including input/output pipes
public class GeminiCliManager
{
    public async Task<bool> InputAsync(string input, CancellationToken cancellationToken)
    {
        var process = new Process();
        var startInfo = new ProcessStartInfo
        {
            FileName = "osascript",
            Arguments = "scripts/gemini-cli-input.scpt \"" + input + "\"",
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
            Console.WriteLine("Failed to execute cli input script.");
            return false;
        }
            
        await process.WaitForExitAsync(cancellationToken);
        return true;
    }
}