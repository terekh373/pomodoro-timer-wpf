using System.Windows.Shell;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PomodoroTimer.Core;
using PomodoroTimer.Properties;
using PomodoroTimer.Services;

namespace PomodoroTimer.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly PomodoroEngine _engine;
    private readonly DispatcherTimer _timer;

    [ObservableProperty] private string _timeText = "";
    [ObservableProperty] private string _titleText = "";
    [ObservableProperty] private string _subtitleText = "";
    [ObservableProperty] private bool _hasName;
    [ObservableProperty] private string _sessionText = "";
    [ObservableProperty] private string _toggleText = "";
    [ObservableProperty] private double _progress;
    [ObservableProperty] private TaskbarItemProgressState _taskbarState;

    private readonly AppServices _services;

    public MainViewModel(PomodoroEngine engine, AppServices services)
    {
        _engine = engine;
        _services = services;
        _engine.Changed += Refresh;
        services.Store.Changed += Refresh; // session names may have been edited

        _timer = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromMilliseconds(200) };
        _timer.Tick += (_, _) => { _engine.Tick(); Refresh(); };
        _timer.Start();

        // After sleep the wall clock has moved on; re-evaluate immediately.
        SystemEvents.PowerModeChanged += (_, e) =>
        {
            if (e.Mode == PowerModes.Resume)
                _timer.Dispatcher.BeginInvoke(() => { _engine.Tick(); Refresh(); });
        };

        Refresh();
    }

    [RelayCommand] private void Toggle() => _engine.Toggle();
    [RelayCommand] private void Reset() => _engine.Reset();
    [RelayCommand] private void Skip() => _engine.Skip();

    private void Refresh()
    {
        var remaining = _engine.Remaining;
        TimeText = TimeFormat.Format(remaining);

        var phaseName = PhaseNames.Get(_engine.Phase);
        SessionText = string.Format(Strings.SessionFormat, _engine.SessionInCycle, _engine.LongBreakInterval);

        // With a user-defined name: name is the title, "<phase> · <session N/M>" goes below it.
        var name = _services.Settings.SessionNames.Resolve(_engine.Phase, _engine.SessionInCycle);
        HasName = name != null;
        TitleText = name ?? phaseName;
        SubtitleText = name != null ? string.Format(Strings.SubtitleFormat, phaseName, SessionText) : "";
        ToggleText = _engine.IsRunning ? Strings.Pause : Strings.Start;

        var total = _engine.Total.TotalSeconds;
        Progress = total > 0 ? 1 - remaining.TotalSeconds / total : 0;
        TaskbarState = _engine.IsRunning ? TaskbarItemProgressState.Normal
            : Progress > 0 ? TaskbarItemProgressState.Paused
            : TaskbarItemProgressState.None;
    }
}
