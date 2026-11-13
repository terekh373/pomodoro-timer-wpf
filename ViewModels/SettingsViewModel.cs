using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PomodoroTimer.Properties;
using PomodoroTimer.Services;

namespace PomodoroTimer.ViewModels;

public sealed record AccentOption(string? Hex, Brush Brush);

public enum HotkeyTarget { Toggle, Skip }

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly AppServices _svc;

    [ObservableProperty] private bool _showAutostartWarning;
    [ObservableProperty] private string _hotkeyWarning = "";

    public SettingsViewModel(AppServices svc)
    {
        _svc = svc;
        Sounds = new[]
        {
            new SoundEventViewModel(svc, SoundEvent.WorkEnd, Strings.SoundWorkEnd),
            new SoundEventViewModel(svc, SoundEvent.BreakEnd, Strings.SoundBreakEnd),
        };
        AccentOptions = new[]
        {
            Accent(null, "#808080"), Accent("#0078D4", null), Accent("#E74856", null), Accent("#F7630C", null),
            Accent("#FFB900", null), Accent("#10893E", null), Accent("#00B7C3", null), Accent("#8764B8", null),
            Accent("#E3008C", null),
        };
        RefreshWarnings();
    }

    private AppSettings S => _svc.Settings;

    public IReadOnlyList<SoundEventViewModel> Sounds { get; }
    public IReadOnlyList<AccentOption> AccentOptions { get; }

    private static AccentOption Accent(string? hex, string? fallback) =>
        new(hex, new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex ?? fallback)));

    private void Set<T>(T current, T value, Action<T> assign, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(current, value)) return;
        assign(value);
        _svc.Store.Save(); // raises Changed → settings are applied immediately
        OnPropertyChanged(name);
        RefreshWarnings();
    }

    // ---- Startup ----
    public bool LaunchWithWindows
    {
        get => S.LaunchWithWindows;
        set => Set(S.LaunchWithWindows, value, v => S.LaunchWithWindows = v);
    }

    /// <summary>0 = open window, 1 = minimize to tray.</summary>
    public int StartModeIndex
    {
        get => S.StartMinimized ? 1 : 0;
        set => Set(StartModeIndex, value, v => S.StartMinimized = v == 1);
    }

    /// <summary>0 = minimize to tray on close, 1 = exit.</summary>
    public int CloseActionIndex
    {
        get => S.CloseToTray ? 0 : 1;
        set => Set(CloseActionIndex, value, v => S.CloseToTray = v == 0);
    }

    public bool AlwaysOnTop
    {
        get => S.AlwaysOnTop;
        set => Set(S.AlwaysOnTop, value, v => S.AlwaysOnTop = v);
    }

    /// <summary>The tray menu can change settings while this window is open; keep the bound controls in sync.</summary>
    public void OnSettingsChangedElsewhere() => OnPropertyChanged(nameof(AlwaysOnTop));

    // ---- Timer ----
    public int WorkMinutes
    {
        get => S.WorkMinutes;
        set => Set(S.WorkMinutes, Math.Clamp(value, AppSettings.MinMinutes, AppSettings.MaxMinutes), v => S.WorkMinutes = v);
    }

    public int ShortBreakMinutes
    {
        get => S.ShortBreakMinutes;
        set => Set(S.ShortBreakMinutes, Math.Clamp(value, AppSettings.MinMinutes, AppSettings.MaxMinutes), v => S.ShortBreakMinutes = v);
    }

    public int LongBreakMinutes
    {
        get => S.LongBreakMinutes;
        set => Set(S.LongBreakMinutes, Math.Clamp(value, AppSettings.MinMinutes, AppSettings.MaxMinutes), v => S.LongBreakMinutes = v);
    }

    public int LongBreakInterval
    {
        get => S.LongBreakInterval;
        set => Set(S.LongBreakInterval, Math.Clamp(value, 1, 20), v => S.LongBreakInterval = v);
    }

    public bool AutoStartBreak
    {
        get => S.AutoStartBreak;
        set => Set(S.AutoStartBreak, value, v => S.AutoStartBreak = v);
    }

    public bool AutoStartWork
    {
        get => S.AutoStartWork;
        set => Set(S.AutoStartWork, value, v => S.AutoStartWork = v);
    }

    public int DailyGoal
    {
        get => S.DailyGoal;
        set => Set(S.DailyGoal, Math.Clamp(value, 1, 100), v => S.DailyGoal = v);
    }

    // ---- Sounds / notifications ----
    public int Volume
    {
        get => S.Volume;
        set => Set(S.Volume, Math.Clamp(value, 0, 100), v => S.Volume = v);
    }

    public bool TickingEnabled
    {
        get => S.TickingEnabled;
        set => Set(S.TickingEnabled, value, v => S.TickingEnabled = v);
    }

    public bool ToastEnabled
    {
        get => S.ToastEnabled;
        set => Set(S.ToastEnabled, value, v => S.ToastEnabled = v);
    }

    public bool ShowWindowOnPhaseEnd
    {
        get => S.ShowWindowOnPhaseEnd;
        set => Set(S.ShowWindowOnPhaseEnd, value, v => S.ShowWindowOnPhaseEnd = v);
    }

    // ---- Appearance ----
    public int ThemeIndex
    {
        get => (int)S.Theme;
        set => Set(ThemeIndex, value, v => S.Theme = (AppThemeMode)v);
    }

    [RelayCommand]
    private void SelectAccent(string? hex)
    {
        S.AccentColor = hex;
        _svc.Store.Save();
    }

    // ---- Hotkeys ----
    public string ToggleHotkeyText => FormatHotkey(S.ToggleHotkey);
    public string SkipHotkeyText => FormatHotkey(S.SkipHotkey);

    /// <summary>Called by the view when the user pressed a combination in a hotkey box (key 0 = clear).</summary>
    public void SetHotkey(HotkeyTarget target, ModifierKeys modifiers, Key key)
    {
        if (key != Key.None && modifiers == ModifierKeys.None)
        {
            HotkeyWarning = Strings.HotkeyNeedModifier;
            return;
        }

        HotkeyWarning = "";
        var hk = key == Key.None ? new HotkeySettings() : new HotkeySettings((uint)modifiers, (int)key);
        if (target == HotkeyTarget.Toggle) S.ToggleHotkey = hk; else S.SkipHotkey = hk;
        _svc.Store.Save();
        OnPropertyChanged(target == HotkeyTarget.Toggle ? nameof(ToggleHotkeyText) : nameof(SkipHotkeyText));
        RefreshWarnings();
    }

    private static string FormatHotkey(HotkeySettings hk)
    {
        if (hk.Key == 0) return Strings.HotkeyNone;
        var parts = new List<string>();
        if ((hk.Modifiers & 2) != 0) parts.Add("Ctrl");
        if ((hk.Modifiers & 1) != 0) parts.Add("Alt");
        if ((hk.Modifiers & 4) != 0) parts.Add("Shift");
        if ((hk.Modifiers & 8) != 0) parts.Add("Win");
        parts.Add(((Key)hk.Key).ToString());
        return string.Join("+", parts);
    }

    // ---- Reset / export / import ----
    [RelayCommand]
    private void ResetDefaults()
    {
        if (MessageBox.Show(Strings.ResetConfirm, Strings.SettingsTitle, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        ReplaceAll(new AppSettings());
    }

    [RelayCommand]
    private void Export()
    {
        var dlg = new SaveFileDialog { Filter = Strings.JsonFilter, FileName = "pomodoro-settings.json" };
        if (dlg.ShowDialog() != true) return;
        try { _svc.Store.ExportTo(dlg.FileName); }
        catch (IOException ex) { MessageBox.Show(ex.Message, Strings.SettingsTitle, MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    [RelayCommand]
    private void Import()
    {
        var dlg = new OpenFileDialog { Filter = Strings.JsonFilter };
        if (dlg.ShowDialog() != true) return;
        try
        {
            ReplaceAll(SettingsService.ImportFrom(dlg.FileName));
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            MessageBox.Show(Strings.ImportError, Strings.SettingsTitle, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ReplaceAll(AppSettings settings)
    {
        _svc.Store.Replace(settings);
        OnPropertyChanged(string.Empty);
        foreach (var s in Sounds) s.Refresh();
        RefreshWarnings();
    }

    /// <summary>Re-check external state (Windows Startup apps, hotkey availability).</summary>
    public void RefreshWarnings()
    {
        ShowAutostartWarning = S.LaunchWithWindows && _svc.Autostart.IsDisabledByUser;

        var busy = !_svc.Hotkeys.IsActive(AppServices.ToggleHotkeyId, S.ToggleHotkey)
                   || !_svc.Hotkeys.IsActive(AppServices.SkipHotkeyId, S.SkipHotkey);
        if (busy) HotkeyWarning = Strings.HotkeyBusy;
        else if (HotkeyWarning == Strings.HotkeyBusy) HotkeyWarning = "";
    }
}
