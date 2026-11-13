using System.Windows.Input;
using PomodoroTimer.Services;
using PomodoroTimer.ViewModels;
using Wpf.Ui.Controls;

namespace PomodoroTimer;

public partial class SettingsWindow : FluentWindow
{
    private readonly SettingsViewModel _vm;

    public SettingsWindow(AppServices services)
    {
        _vm = new SettingsViewModel(services);
        DataContext = _vm;
        InitializeComponent();
        services.Theme.Register(this);
        Activated += (_, _) => _vm.RefreshWarnings(); // user may toggle Startup apps in Windows meanwhile

        // Settings can also change from the tray menu while this dialog is open.
        void OnStoreChanged() => _vm.OnSettingsChangedElsewhere();
        services.Store.Changed += OnStoreChanged;
        Closed += (_, _) => services.Store.Changed -= OnStoreChanged;
    }

    /// <summary>Captures the pressed key combination into the hotkey box.</summary>
    private void OnHotkeyKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift
            or Key.LWin or Key.RWin or Key.ImeProcessed or Key.DeadCharProcessed or Key.None)
            return;

        var target = (string)((System.Windows.Controls.TextBox)sender).Tag == "Toggle"
            ? HotkeyTarget.Toggle
            : HotkeyTarget.Skip;

        if (key is Key.Escape or Key.Back or Key.Delete)
            _vm.SetHotkey(target, ModifierKeys.None, Key.None);
        else
            _vm.SetHotkey(target, Keyboard.Modifiers, key);
    }
}
