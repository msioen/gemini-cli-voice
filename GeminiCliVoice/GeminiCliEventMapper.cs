using GeminiCliVoice.Model;

namespace GeminiCliVoice;

public static class GeminiCliEventMapper
{
    private const string AttributeEventName = "event.name";
    private const string AttributeEventTimestamp = "event.timestamp";
    
    private static readonly Dictionary<string, Func<LogRecord, CliEvent>> _eventParsers = new()
    {
        { "gemini_cli.input_active", ParseInputActive },
        { "gemini_cli.user_prompt", ParseUserPrompt },
        { "gemini_cli.api_request", ParseApiRequest },
        { "gemini_cli.api_response", ParseApiResponse },
        { "gemini_cli.tool_permission_request", ParseToolPermissionRequest },
        { "gemini_cli.tool_call", ParseToolCall },
        { "gemini_cli.api_error", ParseApiError },
    };

    public static CliEvent? Map(LogRecord logRecord)
    {
        var eventName = logRecord.Attributes.GetValueOrDefault(AttributeEventName)?.GetString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(eventName) || !_eventParsers.TryGetValue(eventName, out var parser))
        {
            return null;
        }
        
        return parser(logRecord);
    }

    private static CliInputActiveEvent ParseInputActive(LogRecord logRecord)
    {
        return new CliInputActiveEvent
        {
            EventName = logRecord.Attributes.GetValueOrDefault(AttributeEventName)!.Value.GetString(),
            EventTimestamp = logRecord.Attributes.GetValueOrDefault(AttributeEventTimestamp)!.Value.GetDateTime(),
            Body = logRecord.Body,
            Active = logRecord.Attributes.GetValueOrDefault("active")!.Value.GetBoolean()
        };
    }
    
    private static CliUserPromptEvent ParseUserPrompt(LogRecord logRecord)
    {
        return new CliUserPromptEvent
        {
            EventName = logRecord.Attributes.GetValueOrDefault(AttributeEventName)!.Value.GetString(),
            EventTimestamp = logRecord.Attributes.GetValueOrDefault(AttributeEventTimestamp)!.Value.GetDateTime(),
            Body = logRecord.Body,
            PromptText = logRecord.Attributes.GetValueOrDefault("prompt")?.GetString() ?? string.Empty
        };
    }
    
    private static CliApiRequestEvent ParseApiRequest(LogRecord logRecord)
    {
        return new CliApiRequestEvent
        {
            EventName = logRecord.Attributes.GetValueOrDefault(AttributeEventName)!.Value.GetString(),
            EventTimestamp = logRecord.Attributes.GetValueOrDefault(AttributeEventTimestamp)!.Value.GetDateTime(),
            Body = logRecord.Body,
            RequestText = logRecord.Attributes.GetValueOrDefault("request_text")?.GetString() ?? string.Empty,
        };
    }
    
    private static CliApiResponseEvent ParseApiResponse(LogRecord logRecord)
    {
        return new CliApiResponseEvent
        {
            EventName = logRecord.Attributes.GetValueOrDefault(AttributeEventName)!.Value.GetString(),
            EventTimestamp = logRecord.Attributes.GetValueOrDefault(AttributeEventTimestamp)!.Value.GetDateTime(),
            Body = logRecord.Body,
            ResponseText = logRecord.Attributes.GetValueOrDefault("response_text")?.GetString() ?? string.Empty,
            StatusCode = logRecord.Attributes.GetValueOrDefault("status_code")?.GetInt32() ?? 0,
            DurationMs = logRecord.Attributes.GetValueOrDefault("duration_ms")?.GetInt32() ?? 0,
            Error = logRecord.Attributes.GetValueOrDefault("error")?.GetString() ?? string.Empty,
        };
    }
    
    private static CliToolPermissionRequestEvent ParseToolPermissionRequest(LogRecord logRecord)
    {
        return new CliToolPermissionRequestEvent
        {
            EventName = logRecord.Attributes.GetValueOrDefault(AttributeEventName)!.Value.GetString(),
            EventTimestamp = logRecord.Attributes.GetValueOrDefault(AttributeEventTimestamp)!.Value.GetDateTime(),
            Body = logRecord.Body,
            Type = logRecord.Attributes.GetValueOrDefault("type")?.GetString() ?? string.Empty,
            Title = logRecord.Attributes.GetValueOrDefault("title")?.GetString() ?? string.Empty,
        };
    }
    
    private static CliToolCallEvent ParseToolCall(LogRecord logRecord)
    {
        return new CliToolCallEvent
        {
            EventName = logRecord.Attributes.GetValueOrDefault(AttributeEventName)!.Value.GetString(),
            EventTimestamp = logRecord.Attributes.GetValueOrDefault(AttributeEventTimestamp)!.Value.GetDateTime(),
            Body = logRecord.Body,
            FunctionName = logRecord.Attributes.GetValueOrDefault("function_name")?.GetString() ?? string.Empty,
            FunctionArguments = logRecord.Attributes.GetValueOrDefault("function_args")?.GetString() ?? string.Empty,
        };
    }
    
    private static CliApiErrorEvent ParseApiError(LogRecord logRecord)
    {
        return new CliApiErrorEvent
        {
            EventName = logRecord.Attributes.GetValueOrDefault(AttributeEventName)!.Value.GetString(),
            EventTimestamp = logRecord.Attributes.GetValueOrDefault(AttributeEventTimestamp)!.Value.GetDateTime(),
            Body = logRecord.Body,
            Error = logRecord.Attributes.GetValueOrDefault("error")?.GetString() ?? string.Empty,
            ErrorType = logRecord.Attributes.GetValueOrDefault("error_type")?.GetString() ?? string.Empty,
        };
    }
}