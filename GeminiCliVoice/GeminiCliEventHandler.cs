using GeminiCliVoice.Model;

namespace GeminiCliVoice;

public interface IGeminiCliEventHandler
{
    void Handle(LogRecord logRecord);
    void Handle(CliEvent cliEvent);
}

public class GeminiCliEventHandler : IHostedService, IGeminiCliEventHandler
{
    private readonly KokoroPlayer _kokoroPlayer;
    private readonly SoundPlayer _soundPlayer;
    private readonly WhisperManager _whisperManager;
    
    private CancellationTokenSource _cts = new CancellationTokenSource();

    private int _currPrio = -1;
    private int _queuePrio = -1;

    private Task? _currTask;
    private Func<Task>? _nextTaskFunc;
    
    public GeminiCliEventHandler(
        KokoroPlayer kokoroPlayer,
        SoundPlayer soundPlayer,
        WhisperManager whisperManager)
    {
        _kokoroPlayer = kokoroPlayer;
        _soundPlayer = soundPlayer;
        _whisperManager = whisperManager;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public void Handle(LogRecord logRecord)
    {
        var cliEvent = GeminiCliEventMapper.Map(logRecord);
        if (cliEvent != null)
        {
            Handle(cliEvent);
        }
    }
    
    public void Handle(CliEvent cliEvent)
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