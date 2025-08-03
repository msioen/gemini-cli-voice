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
builder.Services.AddSingleton<GeminiCliManager>();

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

        await eventHandler.ReplayCliEvents(events, cancellationToken);

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

        await eventHandler.ReplayCliEvents(events, cancellationToken);

        return TypedResults.Ok();
    });

app.Run();

public class LogRecord
{
    [JsonPropertyName("body")]
    public string Body { get; set; }
    
    [JsonPropertyName("attributes")]
    public Dictionary<string, JsonElement?> Attributes { get; set; }
}