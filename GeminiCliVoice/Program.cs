using System.Text.Json;
using System.Text.Json.Serialization;
using GeminiCliVoice;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<KokoroPlayer>();
builder.Services.AddSingleton<SoundPlayer>();
builder.Services.AddSingleton<WhisperManager>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/stt", async (WhisperManager whisperManager, CancellationToken cancellationToken) =>
{
    var text = await whisperManager.GetTranscribedMicrophoneInputAsync(4000, cancellationToken);
    return TypedResults.Ok(text);
});

app.MapGet("/tts", async (string text, KokoroPlayer kokoroPlayer, CancellationToken cancellationToken) =>
{
     await kokoroPlayer.PlayAsync(text, cancellationToken);
    return TypedResults.Ok();
});

app.MapGet("/sound", async (string sound, SoundPlayer soundPlayer, CancellationToken cancellationToken) =>
{
    await soundPlayer.PlaySoundAsync(sound, cancellationToken);
    return TypedResults.Ok();
});

app.MapPost("/log", async (HttpContext context, CancellationToken cancellationToken) =>
    {
        using var reader = new StreamReader(context.Request.Body);
        var bodyContent = await reader.ReadToEndAsync(cancellationToken);
        
        //todo properly from stream and such
        var data = JsonSerializer.Deserialize<LogRecord>(bodyContent);
        
        Console.WriteLine("Received log data:");
        Console.WriteLine(bodyContent);
        
        // todo singleton(?) service to handle things
        // do we need some kind of background job? to trigger the sounds and such?
        return TypedResults.Ok();
    })
    .WithName("Log")
    .WithOpenApi();

app.Run();

public class LogRecord
{
    [JsonPropertyName("body")]
    public string Body { get; set; }
    
    [JsonPropertyName("attributes")]
    public Dictionary<string, string> Attributes { get; set; }
}