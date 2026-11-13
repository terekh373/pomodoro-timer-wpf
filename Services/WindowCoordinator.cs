using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using PomodoroTimer.ViewModels;

namespace PomodoroTimer.Services;

/// <summary>Owns the main/mini/settings windows and the transitions between them.</summary>
public sealed class WindowCoordinator
{
    private static readonly TimeSpan TemporaryTopmostDuration = TimeSpan.FromSeconds(10);

    private readonly AppServices _services;
    private readonly DispatcherTimer _temporaryTopmostTimer;
    private bool _temporaryTopmost;
    private MiniWindow? _mini;
    private SettingsWindow? _settings;
    private StatsWindow? _stats;
    private TimersWindow? _timers;

    public WindowCoordinator(AppServices services)
    {
        _services = services;
        ViewModel = new MainViewModel(services.Engine, services);
        Main = new MainWindow(this, services, ViewModel);

        _temporaryTopmostTimer = new DispatcherTimer { Interval = TemporaryTopmostDuration };
        _temporaryTopmostTimer.Tick += (_, _) => EndTemporaryTopmost();
        Main.Activated += (_, _) => EndTemporaryTopmost(); // the user reached the window: no need to stay on top

        services.Store.Changed += ApplyTopmost; // "Поверх всех окон" toggled in settings or the tray menu
        services.PhaseEnded += OnPhaseEnded;
        services.Toast.ActionInvoked += action =>
        {
            if (action == ToastService.ActionShow) ShowMain(); // "Начать" is handled by AppServices
        };

        ApplyTopmost();
    }

    public MainViewModel ViewModel { get; }
    public MainWindow Main { get; }

    /// <summary>Set before shutdown so closing the main window really closes it.</summary>
    public bool IsExiting { get; set; }

    /// <summary>The single source of truth for the main window's Topmost: permanent setting OR temporary alert mode.</summary>
    public bool DesiredTopmost => _services.Settings.AlwaysOnTop || _temporaryTopmost;

    /// <summary>User-initiated: bring the main window up and focus it.</summary>
    public void ShowMain()
    {
        _mini?.Close();
        EndTemporaryTopmost(apply: false);
        Main.BringToFront();
        ApplyTopmost();
    }

    /// <summary>Phase ended: show the main window above everything WITHOUT taking keyboard focus, and flash the taskbar button.</summary>
    private void OnPhaseEnded()
    {
        _mini?.Close(); // expand to the main window, as ShowMain does

        var hwnd = new WindowInteropHelper(Main).EnsureHandle();
        _temporaryTopmost = true;
        ApplyTopmost(); // Topmost before showing, so the window appears above everything

        if (!Main.IsVisible)
        {
            Main.ShowActivated = false;
            Main.Show();
            Main.ShowActivated = true;
        }
        else if (Main.WindowState == WindowState.Minimized)
        {
            NativeMethods.ShowWindow(hwnd, NativeMethods.SwShowNoActivate);
        }

        NativeMethods.FlashUntilForeground(hwnd);

        _temporaryTopmostTimer.Stop();
        _temporaryTopmostTimer.Start();
    }

    private void EndTemporaryTopmost(bool apply = true)
    {
        _temporaryTopmostTimer.Stop();
        if (!_temporaryTopmost) return;
        _temporaryTopmost = false;
        if (apply) ApplyTopmost();
    }

    /// <summary>Applies <see cref="DesiredTopmost"/> to the main window and its owned dialogs (they must stay above it).</summary>
    public void ApplyTopmost()
    {
        var top = DesiredTopmost;
        if (Main.Topmost != top) Main.Topmost = top;
        foreach (Window? w in new Window?[] { _settings, _stats, _timers })
        {
            if (w != null && w.Topmost != top) w.Topmost = top;
        }
    }

    public void ShowMini()
    {
        if (_mini != null) return;
        _mini = new MiniWindow(this, _services, ViewModel);
        _mini.Closed += (_, _) => _mini = null;
        Main.Hide();
        _mini.Show();
    }

    public void OpenSettings()
    {
        ShowMain();
        if (_settings != null)
        {
            _settings.Activate();
            return;
        }

        _settings = new SettingsWindow(_services) { Owner = Main, Topmost = DesiredTopmost };
        _settings.Closed += (_, _) => _settings = null;
        _settings.ShowDialog();
    }

    public void OpenStats()
    {
        ShowMain();
        if (_stats != null)
        {
            _stats.Activate();
            return;
        }

        _stats = new StatsWindow(_services) { Owner = Main, Topmost = DesiredTopmost };
        _stats.Closed += (_, _) => _stats = null;
        _stats.Show();
    }

    public void OpenTimers()
    {
        ShowMain();
        if (_timers != null)
        {
            _timers.Activate();
            return;
        }

        _timers = new TimersWindow(_services) { Owner = Main, Topmost = DesiredTopmost };
        _timers.Closed += (_, _) => _timers = null;
        _timers.Show();
    }

    public void Exit()
    {
        IsExiting = true;
        Application.Current.Shutdown();
    }
}
