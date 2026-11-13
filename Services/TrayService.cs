using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using H.NotifyIcon;
using PomodoroTimer.Core;
using PomodoroTimer.Properties;
using PomodoroTimer.ViewModels;

namespace PomodoroTimer.Services;

/// <summary>Tray icon with live tooltip and a context menu.</summary>
public sealed class TrayService : IDisposable
{
    private readonly PomodoroEngine _engine;
    private readonly TaskbarIcon _icon;
    private readonly MenuItem _toggleItem;
    private readonly MenuItem _topmostItem;
    private readonly DispatcherTimer _timer;

    private readonly AppServices _services;

    public TrayService(AppServices services, WindowCoordinator windows)
    {
        _services = services;
        _engine = services.Engine;

        _toggleItem = new MenuItem();
        _toggleItem.Click += (_, _) => _engine.Toggle();
        var skip = new MenuItem { Header = Strings.Skip };
        skip.Click += (_, _) => _engine.Skip();
        var show = new MenuItem { Header = Strings.TrayShow };
        show.Click += (_, _) => windows.ShowMain();
        var settings = new MenuItem { Header = Strings.Settings };
        settings.Click += (_, _) => windows.OpenSettings();
        _topmostItem = new MenuItem { Header = Strings.AlwaysOnTop, IsCheckable = true };
        _topmostItem.Click += (_, _) =>
        {
            // IsCheckable has already flipped IsChecked; store it (the setting change is applied by the window coordinator).
            services.Settings.AlwaysOnTop = _topmostItem.IsChecked;
            services.Store.Save();
        };
        var exit = new MenuItem { Header = Strings.TrayExit };
        exit.Click += (_, _) => windows.Exit();

        var menu = new ContextMenu();
        menu.Items.Add(_toggleItem);
        menu.Items.Add(skip);
        menu.Items.Add(new Separator());
        menu.Items.Add(show);
        menu.Items.Add(settings);
        menu.Items.Add(_topmostItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(exit);
        menu.Opened += (_, _) =>
        {
            UpdateToggleText();
            _topmostItem.IsChecked = services.Settings.AlwaysOnTop; // may have been changed in the settings window
        };

        _icon = new TaskbarIcon
        {
            IconSource = LoadIcon(),
            ContextMenu = menu,
            ToolTipText = Strings.AppTitle,
            NoLeftClickDelay = true,
        };
        _icon.TrayLeftMouseUp += (_, _) => windows.ShowMain();
        _icon.ForceCreate(enablesEfficiencyMode: false); // main window may stay hidden, so create explicitly

        UpdateToggleText();
        UpdateTooltip();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => UpdateTooltip();
        _timer.Start();
    }

    private void UpdateToggleText() =>
        _toggleItem.Header = _engine.IsRunning ? Strings.Pause : Strings.Start;

    // Windows limits a native tray tooltip to 127 characters.
    private const int MaxTooltipLength = 127;

    private void UpdateTooltip()
    {
        var settings = _services.Settings;
        var phase = PhaseNames.Display(settings, _engine.Phase, _engine.SessionInCycle);
        var time = TimeFormat.Format(_engine.Remaining);
        var paused = !_engine.IsRunning && _engine.Remaining < _engine.Total;

        var line1 = _engine.EndTimeLocal is { } end
            ? string.Format(Strings.TrayRunningFormat, phase, time, end.ToString("HH:mm"))
            : paused
                ? string.Format(Strings.TrayPausedFormat, phase, time)
                : string.Format(Strings.TrayTooltipFormat, phase, time);

        var line2 = string.Format(Strings.TrayNextFormat, _engine.SessionInCycle, _engine.LongBreakInterval,
            PhaseNames.Display(settings, _engine.NextPhase, _engine.NextSessionInCycle), TimeFormat.Minutes(_engine.NextPhaseDuration.TotalMinutes));

        var today = DateTime.Today;
        var doneToday = _services.History.Entries.Where(e => e.Phase == PomodoroPhase.Work && e.CompletedAt.Date == today).ToList();
        var line3 = string.Format(Strings.TrayTodayFormat, doneToday.Count, _services.Settings.DailyGoal,
            TimeFormat.Minutes(doneToday.Sum(e => e.DurationMinutes)));

        var text = string.Join("\n", line1, line2, line3);

        _icon.ToolTipText = text.Length > MaxTooltipLength ? text[..(MaxTooltipLength - 1)] + "…" : text;
    }

    private static BitmapSource LoadIcon()
    {
        var decoder = BitmapDecoder.Create(new Uri("pack://application:,,,/Assets/app.ico"),
            BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        // Prefer the 32 px frame for the tray; fall back to the first one.
        return decoder.Frames.FirstOrDefault(f => f.PixelWidth == 32) ?? decoder.Frames[0];
    }

    public void Dispose()
    {
        _timer.Stop();
        _icon.Dispose(); // removes the icon from the tray
    }
}
