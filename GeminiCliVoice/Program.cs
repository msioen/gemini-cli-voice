using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using GeminiCliVoice;
using GeminiCliVoice.Model;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<KokoroPlayer>();
builder.Services.AddSingleton<SoundPlayer>();
builder.Services.AddSingleton<WhisperManager>();

builder.Services.AddHostedService<GeminiCliEventHandler>();
builder.Services.AddSingleton<IGeminiCliEventHandler, GeminiCliEventHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/stt", 
    async (WhisperManager whisperManager, CancellationToken cancellationToken) =>
    {
        var text = await whisperManager.GetTranscribedMicrophoneInputAsync(4000, cancellationToken);
        return TypedResults.Ok(text);
    }) ;

app.MapGet("/tts", 
    async (string text, KokoroPlayer kokoroPlayer, CancellationToken cancellationToken) =>
    {
         await kokoroPlayer.PlayAsync(text, cancellationToken);
        return TypedResults.Ok();
    });

app.MapGet("/sound", 
    async (string sound, SoundPlayer soundPlayer, CancellationToken cancellationToken) =>
    {
        await soundPlayer.PlaySoundAsync(sound, cancellationToken);
        return TypedResults.Ok();
    });

app.MapPost("/log", 
    async (HttpContext context, [FromServices] IGeminiCliEventHandler eventHandler, CancellationToken cancellationToken) =>
    {
        using var reader = new StreamReader(context.Request.Body);
        var bodyContent = await reader.ReadToEndAsync(cancellationToken);
        
        // Uncomment the following line to log the body content to a file for debugging purposes
        // => can be used together with the /project-replay endpoint to replay the events
        // File.AppendAllLines("project.log", new[] { bodyContent });
        
        var logRecord = JsonSerializer.Deserialize<LogRecord>(bodyContent);
        eventHandler.Handle(logRecord);

        return TypedResults.Ok();
    });

app.MapGet("/otel-replay",
    async (string filePath, [FromServices] IGeminiCliEventHandler eventHandler, CancellationToken cancellationToken) =>
    {
        var parser = new GeminiCliOtelParser(filePath);
        var events = parser.Parse();

        await ReplayCliEvents(events, eventHandler, cancellationToken);

        return TypedResults.Ok();
    });

app.MapGet("/project-replay",
    async (string filePath, [FromServices] IGeminiCliEventHandler eventHandler, CancellationToken cancellationToken) =>
    {
        var logRecordLines = File.ReadAllLines(filePath);
        var events = logRecordLines
            .Select(line => JsonSerializer.Deserialize<LogRecord>(line))
            .Select(GeminiCliEventMapper.Map)
            .Where(e => e != null)
            .OfType<CliEvent>()
            .ToList();

        await ReplayCliEvents(events, eventHandler, cancellationToken);

        return TypedResults.Ok();
    });

app.Run();

static async Task ReplayCliEvents(List<CliEvent> cliEvents, IGeminiCliEventHandler geminiCliEventHandler,
    CancellationToken cancellationToken)
{
    var sw = new Stopwatch();
    sw.Start();

    for (int i = 0; i < cliEvents.Count; i++)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            break;
        }

        var currEvent = cliEvents[i];

        Console.WriteLine($"{i} - {sw.ElapsedMilliseconds} - {currEvent.EventName} - {currEvent.Body}");
        
        // TODO - probably need to signify to the eventhandler that this is a replay event
        // => impacts actions like speech to text input which isn't necessary during replay
        geminiCliEventHandler.Handle(currEvent);

        if (i < cliEvents.Count - 1)
        {
            var nextEvent = cliEvents[i + 1];
            var delay = nextEvent.EventTimestamp - currEvent.EventTimestamp; // todo remove duration handle setup?
            await Task.Delay(delay, cancellationToken);
        }
    }
}

public class LogRecord
{
    [JsonPropertyName("body")]
    public string Body { get; set; }
    
    [JsonPropertyName("attributes")]
    public Dictionary<string, JsonElement?> Attributes { get; set; }
}