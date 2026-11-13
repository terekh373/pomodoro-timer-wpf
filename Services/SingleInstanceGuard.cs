namespace PomodoroTimer.Services;

/// <summary>Allows only one running instance; a second launch signals the first one.</summary>
public sealed class SingleInstanceGuard : IDisposable
{
    private const string MutexName = @"Local\PomodoroTimer.SingleInstance";
    private const string EventName = @"Local\PomodoroTimer.Activate";

    private readonly Mutex _mutex;
    private readonly EventWaitHandle _activateEvent;
    private readonly CancellationTokenSource _cts = new();

    public bool IsFirstInstance { get; }

    public event Action? ActivationRequested;

    public SingleInstanceGuard()
    {
        _mutex = new Mutex(true, MutexName, out var createdNew);
        IsFirstInstance = createdNew;
        _activateEvent = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);

        if (IsFirstInstance)
        {
            var thread = new Thread(Listen) { IsBackground = true, Name = "SingleInstanceListener" };
            thread.Start();
        }
    }

    public void SignalFirstInstance() => _activateEvent.Set();

    private void Listen()
    {
        var handles = new[] { _activateEvent, _cts.Token.WaitHandle };
        while (WaitHandle.WaitAny(handles) == 0)
            ActivationRequested?.Invoke();
    }

    public void Dispose()
    {
        _cts.Cancel();
        if (IsFirstInstance)
            _mutex.ReleaseMutex();
        _mutex.Dispose();
        _activateEvent.Dispose();
        _cts.Dispose();
    }
}
