using System.Windows;
using System.Windows.Input;
using PomodoroTimer.Services;
using PomodoroTimer.ViewModels;

namespace PomodoroTimer;

/// <summary>Small always-on-top draggable window with the time and phase.</summary>
public partial class MiniWindow : Window
{
    private readonly WindowCoordinator _windows;
    private readonly AppServices _services;

    public MiniWindow(WindowCoordinator windows, AppServices services, MainViewModel vm)
    {
        _windows = windows;
        _services = services;
        DataContext = vm;
        InitializeComponent();
        RestorePosition();
        Closing += (_, _) => SavePosition();
    }

    private void RestorePosition()
    {
        var s = _services.Settings;
        var area = SystemParameters.WorkArea;
        var left = s.MiniLeft ?? area.Right - Width - 24;
        var top = s.MiniTop ?? area.Top + 24;
        // Keep it reachable if monitors changed since the position was saved.
        var screen = new Rect(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);
        Left = Math.Clamp(left, screen.Left, Math.Max(screen.Left, screen.Right - Width));
        Top = Math.Clamp(top, screen.Top, Math.Max(screen.Top, screen.Bottom - Height));
    }

    private void SavePosition()
    {
        var s = _services.Settings;
        if (s.MiniLeft == Left && s.MiniTop == Top) return;
        s.MiniLeft = Left;
        s.MiniTop = Top;
        _services.Store.Save();
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            _windows.ShowMain();
            return;
        }

        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove(); // returns when the button is released
            SavePosition();
        }
    }
}
