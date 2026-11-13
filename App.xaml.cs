using System.Windows;
using PomodoroTimer.Services;

namespace PomodoroTimer;

public partial class App : Application
{
    private SingleInstanceGuard? _guard;
    private AppServices? _services;
    private WindowCoordinator? _windows;
    private TrayService? _tray;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _guard = new SingleInstanceGuard();
        if (!_guard.IsFirstInstance)
        {
            _guard.SignalFirstInstance();
            Shutdown();
            return;
        }

        _services = new AppServices();
        _windows = new WindowCoordinator(_services);
        MainWindow = _windows.Main;
        _tray = new TrayService(_services, _windows);

        _guard.ActivationRequested += () => Dispatcher.Invoke(_windows.ShowMain);
        SessionEnding += (_, _) => _windows.IsExiting = true; // don't block logoff by hiding to tray

        // "Minimize to tray" at startup: the window simply stays hidden.
        if (!_services.Settings.StartMinimized)
            _windows.Main.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        _services?.Dispose();
        _guard?.Dispose();
        base.OnExit(e);
    }
}
