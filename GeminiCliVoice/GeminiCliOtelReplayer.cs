using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeminiCliVoice;

// Used to test/replay OpenTelemetry traces for Gemini CLI
// This replayer has some 'naive/simple' parsing from the actual file input
// => will not be used in the actual product but can still be useful to validate behaviour or opentelemetry dumps
public class GeminiCliOtelReplayer
{
    private KokoroPlayer _kokoroPlayer = new KokoroPlayer();
    private SoundPlayer _soundPlayer = new SoundPlayer();

    private CancellationTokenSource _cts = new CancellationTokenSource();

    private int _currPrio = -1;
    private int _queuePrio = -1;

    private Task? _currTask;
    private Func<Task>? _nextTaskFunc;
    
    public Task? CurrTask => _currTask;

    public async Task ReplayFileAsync(string inputFile)
    {
        var parser = new GeminiCliOtelParser(inputFile);
        var events = parser.Parse();

        var sw = new Stopwatch();
        sw.Start();

        for (int i = 0; i < events.Count; i++)
        {
            var currEvent = events[i];
            
            Console.WriteLine($"{i} - {sw.ElapsedMilliseconds} - {currEvent.EventName} - {currEvent.Body}");
            Receive(currEvent);
            
            if (i < events.Count - 1)
            {
                var nextEvent = events[i + 1];
                var delay = nextEvent.EventTimestamp - currEvent.EventTimestamp; // todo remove duration handle setup?
                await Task.Delay(delay);
            }
            else if (CurrTask != null)
            {
                await CurrTask;
            }
        }
    }
    
    private void Receive(CliEvent cliEvent)
    {
        var contentPrio = cliEvent.Prepare();
        if (contentPrio > _currPrio || _currTask == null || _currTask.IsCompleted)
        {
            Console.WriteLine("Processing event: " + cliEvent.EventName + " with priority: " + contentPrio);
            _cts.Cancel(); // probably should await this somehow?
            _currTask = null;
            _cts = new CancellationTokenSource();

            _currPrio = contentPrio;
            _nextTaskFunc = () => cliEvent.HandleAsync(_kokoroPlayer, _soundPlayer, _cts.Token);
        }
        else if (contentPrio > _queuePrio)
        {
            Console.WriteLine("Queueing event: " + cliEvent.EventName + " with priority: " + contentPrio);
            _queuePrio = contentPrio;
            _nextTaskFunc = () => cliEvent.HandleAsync(_kokoroPlayer, _soundPlayer, _cts.Token);
        }

        ProcessNextEventAsync();
    }

    private async void ProcessNextEventAsync()
    {
        if (_currTask is { IsCompleted: false })
        {
            var task = _currTask;
            await task;
            return;
        }
        
        while (true)
        {
            if (_nextTaskFunc != null)
            {
                Console.WriteLine("LOOP: get next task");
                _currTask = _nextTaskFunc();
                
                _nextTaskFunc = null;
                _queuePrio = -1;
                Console.WriteLine("LOOP: await curr task");
                var task = _currTask;
                await task;

                Console.WriteLine("LOOP: done awaiting curr task");
                continue;
            }

            Console.WriteLine("LOOP: break");
            break;
        }
    }
}

public class GeminiCliOtelParser
{
    private readonly string _inputFile;
    
    private readonly Dictionary<string, Func<List<string>, CliEvent>> _eventParsers = new()
    {
        { "gemini_cli.user_prompt", ParseUserPrompt },
        { "gemini_cli.api_request", ParseApiRequest },
        { "gemini_cli.api_response", ParseApiResponse },
        { "gemini_cli.tool_call", ParseToolCall },
        { "gemini_cli.api_error", ParseApiError }
    };

    public GeminiCliOtelParser(string inputFile)
    {
        _inputFile = inputFile;
    }

    public List<CliEvent> Parse()
    {
        var events = new List<CliEvent>();
        var lines = File.ReadAllLines(_inputFile);
        
        // look for lines containing event.name: Str({eventName})
        // if the eventName exists in our dictionary get the relevant lines
        // first line is the previous line starting with LogRecord
        // last line is the next line starting with Flags:
        
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains("event.name: Str("))
            {
                var eventName = lines[i].Split("event.name: Str(")[1].Split(")")[0];
                if (_eventParsers.TryGetValue(eventName, out var parser))
                {
                    var index = i;
                    var eventLines = new List<string> { lines[i] };
                    // Collect the previous line starting with LogRecord
                    while(index > 0 && !lines[index - 1].StartsWith("LogRecord"))
                    {
                        eventLines.Insert(0, lines[index - 1]);
                        index--;
                    }
                    // Add the LogRecord line
                    if (index >= 0)
                    {
                        eventLines.Insert(0, lines[index]);
                    }
                    
                    // Collect lines until we hit the next event or the end of the file
                    while (i < lines.Length && !lines[i].StartsWith("Flags:"))
                    {
                        eventLines.Add(lines[i]);
                        i++;
                    }
                    // Add the last line before Flags
                    if (i < lines.Length)
                    {
                        eventLines.Add(lines[i]);
                    }
                    
                    var cliEvent = parser(eventLines);
                    events.Add(cliEvent);
                }
            }
        }

        return events;
    }
    
    private static CliUserPromptEvent ParseUserPrompt(List<string> lines)
    {
        // ```
        // LogRecord #0
        // ObservedTimestamp: 2025-07-27 08:52:48.314 +0000 UTC
        // Timestamp: 2025-07-27 08:52:48.314 +0000 UTC
        // SeverityText: 
        // SeverityNumber: Unspecified(0)
        // Body: Str(User prompt. Length: 53.)
        // Attributes:
        //     -> session.id: Str(b9280381-505c-433a-a0f5-731345d35f87)
        //     -> event.name: Str(gemini_cli.user_prompt)
        //     -> event.timestamp: Str(2025-07-27T08:52:48.314Z)
        //     -> prompt_length: Int(53)
        //     -> prompt: Str(...)
        // Trace ID: 
        // Span ID: 
        // Flags: 0
        //     ```
        var cliEvent = new CliUserPromptEvent();
        foreach (var line in lines)
        {
            if (line.StartsWith("Body: Str("))
            {
                cliEvent.Body = line.Split("Str(")[1][..^1];
            }
            else if (line.StartsWith("     -> event.name: Str("))
            {
                cliEvent.EventName = line.Split("Str(")[1].Split(")")[0];
            }
            else if (line.StartsWith("     -> event.timestamp: Str("))
            {
                cliEvent.EventTimestamp = DateTime.Parse(line.Split("Str(")[1].Split(")")[0]);
            }
            else if (line.StartsWith("     -> prompt: Str("))
            {
                cliEvent.PromptText = line.Split("Str(")[1][..^1];
            }
        }
        return cliEvent;
    }

    private static CliApiRequestEvent ParseApiRequest(List<string> lines)
    {
        // ```
        // LogRecord #1
        // ObservedTimestamp: 2025-07-27 08:52:48.582 +0000 UTC
        // Timestamp: 2025-07-27 08:52:48.582 +0000 UTC
        // SeverityText: 
        // SeverityNumber: Unspecified(0)
        // Body: Str(API request to gemini-2.5-pro.)
        // Attributes:
        //      -> session.id: Str(b9280381-505c-433a-a0f5-731345d35f87)
        //      -> event.name: Str(gemini_cli.api_request)
        //      -> event.timestamp: Str(2025-07-27T08:52:48.581Z)
        //      -> model: Str(gemini-2.5-pro)
        //      -> prompt_id: Str(b9280381-505c-433a-a0f5-731345d35f87########0)
        //      -> request_text: Str(...)
        // Trace ID: 
        // Span ID: 
        // Flags: 0
        // ```
        var cliEvent = new CliApiRequestEvent();
        foreach (var line in lines)
        {
            if (line.StartsWith("Body: Str("))
            {
                cliEvent.Body = line.Split("Str(")[1][..^1];
            }
            else if (line.StartsWith("     -> event.name: Str("))
            {
                cliEvent.EventName = line.Split("Str(")[1].Split(")")[0];
            }
            else if (line.StartsWith("     -> event.timestamp: Str("))
            {
                cliEvent.EventTimestamp = DateTime.Parse(line.Split("Str(")[1].Split(")")[0]);
            }
            else if (line.StartsWith("     -> request_text: Str("))
            {
                cliEvent.RequestText = line.Split("Str(")[1][..^1];
            }
        }

        return cliEvent;
    }
    
    private static CliApiResponseEvent ParseApiResponse(List<string> lines)
    {
        // ```
        // LogRecord #2
        // ObservedTimestamp: 2025-07-27 08:52:52.013 +0000 UTC
        // Timestamp: 2025-07-27 08:52:52.013 +0000 UTC
        // SeverityText: 
        // SeverityNumber: Unspecified(0)
        // Body: Str(API response from gemini-2.5-pro. Status: 200. Duration: 3430ms.)
        // Attributes:
        //      -> session.id: Str(b9280381-505c-433a-a0f5-731345d35f87)
        //      -> event.name: Str(gemini_cli.api_response)
        //      -> event.timestamp: Str(2025-07-27T08:52:52.013Z)
        //      -> model: Str(gemini-2.5-pro)
        //      -> status_code: Int(200)
        //      -> duration_ms: Int(3430)
        //      -> error: Empty()
        //      -> input_token_count: Int(12004)
        //      -> output_token_count: Int(18)
        //      -> cached_content_token_count: Int(3482)
        //      -> thoughts_token_count: Int(93)
        //      -> tool_token_count: Int(0)
        //      -> total_token_count: Int(12115)
        //      -> response_text: Str(...)
        //      -> prompt_id: Str(b9280381-505c-433a-a0f5-731345d35f87########0)
        //      -> auth_type: Str(oauth-personal)
        //      -> http.status_code: Int(200)
        // Trace ID: 
        // Span ID: 
        // Flags: 0
        // ```
        var cliEvent = new CliApiResponseEvent();
        foreach (var line in lines)
        {
            if (line.StartsWith("Body: Str("))
            {
                cliEvent.Body = line.Split("Str(")[1][..^1];
            }
            else if (line.StartsWith("     -> event.name: Str("))
            {
                cliEvent.EventName = line.Split("Str(")[1].Split(")")[0];
            }
            else if (line.StartsWith("     -> event.timestamp: Str("))
            {
                cliEvent.EventTimestamp = DateTime.Parse(line.Split("Str(")[1].Split(")")[0]);
            }
            else if (line.StartsWith("     -> status_code: Int("))
            {
                cliEvent.StatusCode = int.Parse(line.Split("Int(")[1].Split(")")[0]);
            }
            else if (line.StartsWith("     -> duration_ms: Int("))
            {
                cliEvent.DurationMs = int.Parse(line.Split("Int(")[1].Split(")")[0]);
            }
            else if (line.StartsWith("     -> error: Str("))
            {
                cliEvent.Error = line.Split("Str(")[1][..^1];
            }
            else if (line.StartsWith("     -> response_text: Str("))
            {
                cliEvent.ResponseText = line.Split("Str(")[1][..^1];
            }
        }

        return cliEvent;
    }
    
    private static CliToolCallEvent ParseToolCall(List<string> lines)
    {
        // ```
        // LogRecord #1
        // ObservedTimestamp: 2025-07-27 08:53:43.08 +0000 UTC
        // Timestamp: 2025-07-27 08:53:43.08 +0000 UTC
        // SeverityText: 
        // SeverityNumber: Unspecified(0)
        // Body: Str(Tool call: glob. Success: true. Duration: 55ms.)
        // Attributes:
        //     -> session.id: Str(b9280381-505c-433a-a0f5-731345d35f87)
        //     -> event.name: Str(gemini_cli.tool_call)
        //     -> event.timestamp: Str(2025-07-27T08:53:43.080Z)
        //     -> function_name: Str(glob)
        //     -> function_args: Str({
        //     "pattern": "**",
        //     "path": "smartscreen/"
        // })
        // -> duration_ms: Int(55)
        //     -> success: Bool(true)
        //     -> decision: Empty()
        //     -> error: Empty()
        //     -> error_type: Empty()
        //     -> prompt_id: Str(b9280381-505c-433a-a0f5-731345d35f87########0)
        // Trace ID: 
        // Span ID: 
        // Flags: 0
        // ```
        var cliEvent = new CliToolCallEvent();
        foreach (var line in lines)
        {
            if (line.StartsWith("Body: Str("))
            {
                cliEvent.Body = line.Split("Str(")[1][..^1];
            }
            else if (line.StartsWith("     -> event.name: Str("))
            {
                cliEvent.EventName = line.Split("Str(")[1].Split(")")[0];
            }
            else if (line.StartsWith("     -> event.timestamp: Str("))
            {
                cliEvent.EventTimestamp = DateTime.Parse(line.Split("Str(")[1].Split(")")[0]);
            }
            else if (line.StartsWith("     -> function_name: Str("))
            {
                cliEvent.FunctionName = line.Split("Str(")[1][..^1];
            }
            else if (line.StartsWith("     -> function_args: Str("))
            {
                cliEvent.FunctionArguments = line.Split("Str(")[1][..^1];
            }
        }
        return cliEvent;
    }

    private static CliApiErrorEvent ParseApiError(List<string> lines)
    {
        // ```
        // LogRecord #0
        // ObservedTimestamp: 2025-07-27 08:57:03.076 +0000 UTC
        // Timestamp: 2025-07-27 08:57:03.076 +0000 UTC
        // SeverityText: 
        // SeverityNumber: Unspecified(0)
        // Body: Str(API error for gemini-2.5-flash. Error: Please submit a new query to continue with the Flash model.. Duration: 4870ms.)
        // Attributes:
        //     -> session.id: Str(b9280381-505c-433a-a0f5-731345d35f87)
        //     -> event.name: Str(gemini_cli.api_error)
        //     -> event.timestamp: Str(2025-07-27T08:57:03.076Z)
        //     -> model: Str(gemini-2.5-flash)
        //     -> error: Str(Please submit a new query to continue with the Flash model.)
        //     -> error_type: Str(Error)
        //     -> status_code: Empty()
        //     -> duration_ms: Int(4870)
        //     -> prompt_id: Str(b9280381-505c-433a-a0f5-731345d35f87########0)
        //     -> auth_type: Str(oauth-personal)
        //     -> error.message: Str(Please submit a new query to continue with the Flash model.)
        //     -> model_name: Str(gemini-2.5-flash)
        //     -> duration: Int(4870)
        //     -> error.type: Str(Error)
        // Trace ID: 
        // Span ID: 
        // Flags: 0
        // ```
        var cliEvent = new CliApiErrorEvent();
        foreach (var line in lines)
        {
            if (line.StartsWith("Body: Str("))
            {
                cliEvent.Body = line.Split("Str(")[1][..^1];
            }
            else if (line.StartsWith("     -> event.name: Str("))
            {
                cliEvent.EventName = line.Split("Str(")[1].Split(")")[0];
            }
            else if (line.StartsWith("     -> event.timestamp: Str("))
            {
                cliEvent.EventTimestamp = DateTime.Parse(line.Split("Str(")[1].Split(")")[0]);
            }
            else if (line.StartsWith("     -> error: Str("))
            {
                cliEvent.Error = line.Split("Str(")[1][..^1];
            }
            else if (line.StartsWith("     -> error_type: Str("))
            {
                cliEvent.ErrorType = line.Split("Str(")[1][..^1];
            }
            else if (line.StartsWith("     -> error.message: Str("))
            {
                cliEvent.ErrorMessage = line.Split("Str(")[1][..^1];
            }
        }
        return cliEvent;
    }
}


public abstract class CliEvent
{
    public string EventName { get; set; }
    public DateTime EventTimestamp { get; set; }
    public string Body{ get; set; }
    
    public abstract int Prepare();
    public abstract Task HandleAsync(KokoroPlayer ttsPlayer, SoundPlayer soundPlayer, CancellationToken cancellationToken);
}

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

public class CliApiRequestEvent : CliEvent
{
    public string RequestText { get; set; }

    public override int Prepare()
    {
        return 0;
    }
    
    public override Task HandleAsync(KokoroPlayer ttsPlayer, SoundPlayer soundPlayer, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public class CliApiResponseEvent : CliEvent
{
    private string _parsedResponse;
    
    public int StatusCode { get; set; }
    public int DurationMs { get; set; }
    public string Error { get; set; }
    public string ResponseText { get; set; }
    
    public override int Prepare()
    {
        var response = JsonSerializer.Deserialize<List<GeminiResponse>>(ResponseText);
        var parts = response?
            .SelectMany(r => r.Candidates)
            .SelectMany(x => x.Content.Parts.Where(y => !string.IsNullOrWhiteSpace(y.Text)))
            .Select(x => x.Text)
            .ToList();
        if (parts != null && parts.Any())
        {
            _parsedResponse = string.Join(" ", parts);
            return 5;
        }
        
        return 0;
    }
    
    public override Task HandleAsync(KokoroPlayer ttsPlayer, SoundPlayer soundPlayer, CancellationToken cancellationToken)
    {
        Console.WriteLine("starting to play (response): " + _parsedResponse);
        
        return ttsPlayer.PlayAsync(_parsedResponse, cancellationToken);
    }
}

public class CliToolCallEvent : CliEvent
{
    public string FunctionName{ get; set; }
    public string FunctionArguments { get; set; }
    public bool Success { get; set; }
    public string Decision { get; set; }
    public string Error { get; set; }
    public string ErrorType { get; set; }
    
    public override int Prepare()
    {
        return 1;
    }
    
    public override Task HandleAsync(KokoroPlayer ttsPlayer, SoundPlayer soundPlayer, CancellationToken cancellationToken)
    {
        Console.WriteLine($"starting to play (tool): Tool call: {FunctionName}");
        
        return ttsPlayer.PlayAsync($"Tool call: {FunctionName}", cancellationToken);
    }
}

public class CliApiErrorEvent : CliEvent
{
    public string Error { get; set; }
    public string ErrorType { get; set; }
    public string ErrorMessage { get; set; }
    
    public override int Prepare()
    {
        return 5;
    }
    
    public override Task HandleAsync(KokoroPlayer ttsPlayer, SoundPlayer soundPlayer, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public class GeminiResponse
{
    [JsonPropertyName("candidates")]
    public List<GeminiResponseCandidate> Candidates { get; set; }
}

public class GeminiResponseCandidate
{
    [JsonPropertyName("content")]
    public GeminiResponseContent Content { get; set; }
}

public class GeminiResponseContent
{
    [JsonPropertyName("parts")]
    public List<GeminiResponseContentPart> Parts { get; set; }
}

public class GeminiResponseContentPart
{
    [JsonPropertyName("text")]
    public string Text { get; set; }
}