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

    private Task? _currTask;
    private Queue<(Func<Task> Task, bool SkipIfNotLastTask)> _nextTasks = new Queue<(Func<Task>, bool)>();

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
                else
                {
                    // ensure our replay only resets when the last replay event is processed
                    var endTcs = new TaskCompletionSource();
                    _nextTasks.Enqueue((() =>
                    {
                        endTcs.SetResult();
                        return Task.CompletedTask;
                    }, false));
                    await endTcs.Task;
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
    
    public void Handle(CliEvent cliEvent)
    {
        var contentPrio = cliEvent.Prepare();
        _nextTasks.Enqueue((() => cliEvent.HandleAsync(_context, _cts.Token), cliEvent is CliInputActiveEvent));
        
        ProcessNextEventAsync();
    }
    
    private async void ProcessNextEventAsync()
    {
        if (_currTask is { IsCompleted: false })
        {
            await AwaitCurrentTaskAsync();
            return;
        }
        
        while (_nextTasks.TryDequeue(out var nextTask))
        {
            if (nextTask.SkipIfNotLastTask && _nextTasks.Count > 0)
            {
                // Skip this task if there are more tasks in the queue
                Console.WriteLine("Skipping task because there are more tasks in the queue.");
                continue;
            }
            
            Console.WriteLine("LOOP: get next task");
            _currTask = nextTask.Task();
            
            Console.WriteLine("LOOP: await curr task");
            await AwaitCurrentTaskAsync();

            Console.WriteLine("LOOP: done awaiting curr task");
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