using System.ComponentModel;
using System.Windows;
using PomodoroTimer.Services;
using PomodoroTimer.ViewModels;
using Wpf.Ui.Controls;

namespace PomodoroTimer;

public partial class MainWindow : FluentWindow
{
    private readonly WindowCoordinator _windows;
    private readonly AppServices _services;

    public MainWindow(WindowCoordinator windows, AppServices services, MainViewModel vm)
    {
        _windows = windows;
        _services = services;
        DataContext = vm;
        InitializeComponent();
        services.Theme.Register(this);    }

    private void OnSettingsClick(object sender, RoutedEventArgs e) => _windows.OpenSettings();

    private void OnTimersClick(object sender, RoutedEventArgs e) => _windows.OpenTimers();

    private void OnStatsClick(object sender, RoutedEventArgs e) => _windows.OpenStats();

    private void OnMiniClick(object sender, RoutedEventArgs e) => _windows.ShowMini();

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_windows.IsExiting && _services.Settings.CloseToTray)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnClosing(e);
    }

    public void BringToFront()
    {
        if (!IsVisible) Show();
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        Activate();
        // Topmost toggle reliably raises the window above other apps; afterwards go back to the desired
        // state ("Поверх всех окон" must survive being shown again).
        Topmost = true;
        Topmost = _windows.DesiredTopmost;
        Focus();
    }
}
