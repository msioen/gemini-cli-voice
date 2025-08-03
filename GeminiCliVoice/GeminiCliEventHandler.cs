using System.Diagnostics;
using GeminiCliVoice.Model;

namespace GeminiCliVoice;

public interface IGeminiCliEventHandler
{
    Task ReplayCliEvents(List<CliEvent> cliEvents, CancellationToken cancellationToken);
    void Handle(LogRecord logRecord);
    void Handle(CliEvent cliEvent);
}

public class GeminiCliEventHandler : IHostedService, IGeminiCliEventHandler
{
    private readonly Context _context;
    
    private CancellationTokenSource _cts = new CancellationTokenSource();

    private int _currPrio = -1;
    private int _queuePrio = -1;

    private Task? _currTask;
    private Func<Task>? _nextTaskFunc;

    public GeminiCliEventHandler(
        KokoroPlayer kokoroPlayer,
        SoundPlayer soundPlayer,
        WhisperManager whisperManager,
        GeminiCliManager geminiCliManager)
    {
        _context = new Context
        {
            KokoroPlayer = kokoroPlayer,
            SoundPlayer = soundPlayer,
            WhisperManager = whisperManager,
            GeminiCliManager = geminiCliManager
        };
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
    
    public async Task ReplayCliEvents(List<CliEvent> cliEvents, CancellationToken cancellationToken)
    {
        var sw = new Stopwatch();
        sw.Start();

        _context.IsReplayMode = true;

        try
        {
            for (int i = 0; i < cliEvents.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var currEvent = cliEvents[i];
                Console.WriteLine($"{i} - {sw.ElapsedMilliseconds} - {currEvent.EventName} - {currEvent.Body}");
                Handle(currEvent);

                if (i < cliEvents.Count - 1)
                {
                    var nextEvent = cliEvents[i + 1];
                    var delay = nextEvent.EventTimestamp - currEvent.EventTimestamp; // todo remove duration handle setup?
                    await Task.Delay(delay, cancellationToken);
                }
                else if (_currTask != null && !_currTask.IsCompleted)
                {
                    await AwaitCurrentTaskAsync();
                }
            }
        }
        finally
        {
            _context.IsReplayMode = false;
        }
    }

    public void Handle(LogRecord logRecord)
    {
        var cliEvent = GeminiCliEventMapper.Map(logRecord);
        if (cliEvent != null)
        {
            Handle(cliEvent);
        }
    }
    
    // TODO - all events of same prio should (optionally) be queued, now we risk losing critical events for e2e behaviour
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
            _nextTaskFunc = () => cliEvent.HandleAsync(_context, _cts.Token);
        }
        else if (contentPrio > _queuePrio)
        {
            Console.WriteLine("Queueing event: " + cliEvent.EventName + " with priority: " + contentPrio);
            _queuePrio = contentPrio;
            _nextTaskFunc = () => cliEvent.HandleAsync(_context, _cts.Token);
        }

        ProcessNextEventAsync();
    }
    
    private async void ProcessNextEventAsync()
    {
        if (_currTask is { IsCompleted: false })
        {
            await AwaitCurrentTaskAsync();
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
                await AwaitCurrentTaskAsync();

                Console.WriteLine("LOOP: done awaiting curr task");
                continue;
            }

            Console.WriteLine("LOOP: break");
            break;
        }
    }
    
    private async Task AwaitCurrentTaskAsync()
    {
        if (_currTask is { IsCompleted: false })
        {
            try
            {
                var task = _currTask;
                await task;
            }
            catch (OperationCanceledException)
            {
                // Task was cancelled, ignore
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in current task: " + e.Message);
            }
        }
    }
}