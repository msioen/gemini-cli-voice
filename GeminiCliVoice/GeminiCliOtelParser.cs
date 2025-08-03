using GeminiCliVoice.Model;

namespace GeminiCliVoice;

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