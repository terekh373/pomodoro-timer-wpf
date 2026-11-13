using System.IO;
using System.Windows.Threading;
using PomodoroTimer.Core;
using PomodoroTimer.Properties;
using PomodoroTimer.ViewModels;

namespace PomodoroTimer.Services;

/// <summary>Composition root: owns all services and re-applies settings whenever they change.</summary>
public sealed class AppServices : IDisposable
{
    public const int ToggleHotkeyId = 1;
    public const int SkipHotkeyId = 2;

    private readonly TimerOptions _options = new();
    private readonly DispatcherTimer _tickTimer;
    private int _lastTickSecond = -1;

    public SettingsService Store { get; } = new();
    public AutostartService Autostart { get; } = new();
    public SoundService Sound { get; }
    public ToastService Toast { get; } = new();
    public ThemeService Theme { get; } = new();
    public HotkeyService Hotkeys { get; } = new();
    public HistoryService History { get; } = new();
    public PomodoroEngine Engine { get; }

    public AppSettings Settings => Store.Settings;

    public AppServices()
    {
        Store.Load(); // first run: creates the file with defaults (autostart on)
        History.Load();

        Sound = new SoundService(Store);
        CopyTimerOptions();
        Engine = new PomodoroEngine(_options);
        Engine.PhaseCompleted += OnPhaseCompleted;
        Toast.ActionInvoked += action =>
        {
            if (action == ToastService.ActionStart && !Engine.IsRunning) Engine.Start();
        };

        Store.Changed += Apply;
        Apply();

        _tickTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _tickTimer.Tick += (_, _) => TickSound();
        _tickTimer.Start();
    }

    private void CopyTimerOptions()
    {
        var s = Settings;
        _options.WorkMinutes = s.WorkMinutes;
        _options.ShortBreakMinutes = s.ShortBreakMinutes;
        _options.LongBreakMinutes = s.LongBreakMinutes;
        _options.LongBreakInterval = s.LongBreakInterval;
        _options.AutoStartBreak = s.AutoStartBreak;
        _options.AutoStartWork = s.AutoStartWork;
    }

    private void Apply()
    {
        Autostart.Sync(Settings.LaunchWithWindows); // also re-registers if the exe was moved
        CopyTimerOptions();
        Engine.ApplyOptions();
        Theme.Apply(Settings);
        Hotkeys.Register(ToggleHotkeyId, Settings.ToggleHotkey, () => Engine.Toggle());
        Hotkeys.Register(SkipHotkeyId, Settings.SkipHotkey, () => Engine.Skip());
    }

    private void OnPhaseCompleted(PomodoroPhase finished, int session, TimeSpan duration)
    {
        try
        {
            // Every phase is logged with its resolved name (null when the user gave none).
            History.Add(finished, duration, Settings.SessionNames.Resolve(finished, session));
        }
        catch (IOException)
        {
            // Disk problems must not break the timer; the session just isn't logged.
        }

        Sound.Play(finished == PomodoroPhase.Work ? SoundEvent.WorkEnd : SoundEvent.BreakEnd);

        if (Settings.ToastEnabled)
        {
            var title = finished == PomodoroPhase.Work ? Strings.ToastWorkEnded : Strings.ToastBreakEnded;
            var finishedName = PhaseNames.Display(Settings, finished, session);
            var next = string.Format(Strings.ToastNextFormat,
                PhaseNames.Display(Settings, Engine.Phase, Engine.SessionInCycle),
                TimeFormat.Minutes(Engine.Total.TotalMinutes));
            // The engine starts the next phase right after this event when auto-start is on: no "Начать" then.
            var autoStarts = Engine.Phase == PomodoroPhase.Work ? Settings.AutoStartWork : Settings.AutoStartBreak;
            Toast.Show(title, finishedName, next, offerStart: !autoStarts);
        }

        if (Settings.ShowWindowOnPhaseEnd)
            PhaseEnded?.Invoke();
    }

    /// <summary>Raised after a phase ended (and the "show the timer window" setting is on). Windows subscribe to it.</summary>
    public event Action? PhaseEnded;

    private void TickSound()
    {
        if (!Settings.TickingEnabled || !Engine.IsRunning || Engine.Phase != PomodoroPhase.Work)
        {
            _lastTickSecond = -1;
            return;
        }

        var second = (int)Engine.Remaining.TotalSeconds;
        if (second != _lastTickSecond)
        {
            _lastTickSecond = second;
            Sound.PlayTick();
        }
    }

    public void Dispose()
    {
        _tickTimer.Stop();
        Hotkeys.Dispose();
    }
}
