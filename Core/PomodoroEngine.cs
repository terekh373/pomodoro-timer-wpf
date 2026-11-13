namespace PomodoroTimer.Core;

public enum PomodoroPhase { Work, ShortBreak, LongBreak }

public sealed class TimerOptions
{
    public int WorkMinutes { get; set; } = 25;
    public int ShortBreakMinutes { get; set; } = 5;
    public int LongBreakMinutes { get; set; } = 15;
    public int LongBreakInterval { get; set; } = 4;
    public bool AutoStartBreak { get; set; }
    public bool AutoStartWork { get; set; }
}

/// <summary>
/// Pomodoro state machine. Remaining time is always derived from a stored target
/// end time (wall clock), so it stays correct across PC sleep/resume.
/// </summary>
public sealed class PomodoroEngine
{
    private readonly TimerOptions _options;
    private DateTime _endUtc;
    private TimeSpan _pausedRemaining;
    private TimeSpan _phaseTotal;

    public PomodoroEngine(TimerOptions options)
    {
        _options = options;
        Reset();
    }

    public PomodoroPhase Phase { get; private set; }
    public int SessionInCycle { get; private set; }
    public bool IsRunning { get; private set; }
    public int LongBreakInterval => _options.LongBreakInterval;

    public TimeSpan Total => PhaseDuration(Phase);

    public TimeSpan Remaining => IsRunning
        ? Max(TimeSpan.Zero, _endUtc - DateTime.UtcNow)
        : _pausedRemaining;

    /// <summary>Raised when a phase ran to its end (not on Skip/Reset).</summary>
    public event Action<PomodoroPhase, int, TimeSpan>? PhaseCompleted; // phase, its SessionInCycle, its length
    public event Action? Changed;

    public void Start()
    {
        if (IsRunning) return;
        _endUtc = DateTime.UtcNow + _pausedRemaining;
        IsRunning = true;
        Changed?.Invoke();
    }

    public void Pause()
    {
        if (!IsRunning) return;
        _pausedRemaining = Remaining;
        IsRunning = false;
        Changed?.Invoke();
    }

    public void Toggle()
    {
        if (IsRunning) Pause(); else Start();
    }

    public void Reset()
    {
        IsRunning = false;
        Phase = PomodoroPhase.Work;
        SessionInCycle = 1;
        _pausedRemaining = Total;
        _phaseTotal = Total;
        Changed?.Invoke();
    }

    public void Skip() => AdvancePhase();

    /// <summary>
    /// Call after the shared <see cref="TimerOptions"/> were modified. The elapsed part of the
    /// current phase is kept; only the phase length changes.
    /// </summary>
    public void ApplyOptions()
    {
        if (SessionInCycle > _options.LongBreakInterval)
            SessionInCycle = _options.LongBreakInterval;

        var elapsed = _phaseTotal - Remaining;
        var remaining = Max(TimeSpan.Zero, Total - elapsed);
        if (IsRunning) _endUtc = DateTime.UtcNow + remaining;
        else _pausedRemaining = Max(remaining, TimeSpan.FromSeconds(1)); // paused: never show 00:00
        _phaseTotal = Total;
        Changed?.Invoke();
    }

    /// <summary>Call periodically (and after resume from sleep) to detect phase end.</summary>
    public void Tick()
    {
        if (IsRunning && DateTime.UtcNow >= _endUtc)
        {
            var finished = Phase;
            var duration = Total;
            var session = SessionInCycle;
            AdvancePhase();
            PhaseCompleted?.Invoke(finished, session, duration);

            var autoStart = Phase == PomodoroPhase.Work ? _options.AutoStartWork : _options.AutoStartBreak;
            if (autoStart) Start();
        }
    }

    /// <summary>Phase that follows the current one.</summary>
    public PomodoroPhase NextPhase => Phase switch
    {
        PomodoroPhase.Work => SessionInCycle >= _options.LongBreakInterval
            ? PomodoroPhase.LongBreak
            : PomodoroPhase.ShortBreak,
        _ => PomodoroPhase.Work,
    };

    public TimeSpan NextPhaseDuration => PhaseDuration(NextPhase);

    /// <summary>SessionInCycle value that will be current in <see cref="NextPhase"/>.</summary>
    public int NextSessionInCycle => Phase switch
    {
        PomodoroPhase.ShortBreak => SessionInCycle + 1,
        PomodoroPhase.LongBreak => 1,
        _ => SessionInCycle,
    };

    /// <summary>Local time when the running phase ends (null when not running).</summary>
    public DateTime? EndTimeLocal => IsRunning ? _endUtc.ToLocalTime() : null;

    private void AdvancePhase()
    {
        var next = NextPhase;
        switch (Phase)
        {
            case PomodoroPhase.ShortBreak:
                SessionInCycle++;
                break;
            case PomodoroPhase.LongBreak:
                SessionInCycle = 1;
                break;
        }
        Phase = next;

        IsRunning = false;
        _pausedRemaining = Total;
        _phaseTotal = Total;
        Changed?.Invoke();
    }

    private TimeSpan PhaseDuration(PomodoroPhase phase) => TimeSpan.FromMinutes(phase switch
    {
        PomodoroPhase.Work => _options.WorkMinutes,
        PomodoroPhase.ShortBreak => _options.ShortBreakMinutes,
        _ => _options.LongBreakMinutes,
    });

    private static TimeSpan Max(TimeSpan a, TimeSpan b) => a > b ? a : b;
}
