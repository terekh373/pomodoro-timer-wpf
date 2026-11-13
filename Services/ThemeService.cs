using System.Windows.Media;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace PomodoroTimer.Services;

/// <summary>Applies light/dark/system theme and accent color to the app and its windows.</summary>
public sealed class ThemeService
{
    private readonly List<FluentWindow> _windows = new();
    private AppSettings _current = new();
    private (AppThemeMode, string?)? _appliedKey;

    public ThemeService()
    {
        // The system-theme watcher resets the accent to the system one; put the custom one back.
        ApplicationThemeManager.Changed += (_, _) => ApplyAccent();
    }

    public void Register(FluentWindow window)
    {
        _windows.Add(window);
        window.Closed += (_, _) => _windows.Remove(window);
        UpdateWatchers();
    }

    public void Apply(AppSettings settings)
    {
        _current = settings;

        // Settings are saved on every keystroke in some dialogs; don't re-apply an unchanged theme each time.
        var key = (settings.Theme, settings.AccentColor);
        if (_appliedKey == key) return;
        _appliedKey = key;

        var theme = settings.Theme switch
        {
            AppThemeMode.Light => ApplicationTheme.Light,
            AppThemeMode.Dark => ApplicationTheme.Dark,
            _ => ApplicationThemeManager.GetSystemTheme() == SystemTheme.Dark ? ApplicationTheme.Dark : ApplicationTheme.Light,
        };
        ApplicationThemeManager.Apply(theme, WindowBackdropType.Mica, updateAccent: false);
        ApplyAccent();
        UpdateWatchers();
    }

    private void ApplyAccent()
    {
        if (TryParse(_current.AccentColor, out var color))
            ApplicationAccentColorManager.Apply(color, ApplicationThemeManager.GetAppTheme());
        else
            ApplicationAccentColorManager.ApplySystemAccent();
    }

    private void UpdateWatchers()
    {
        foreach (var w in _windows)
        {
            if (_current.Theme == AppThemeMode.System)
                SystemThemeWatcher.Watch(w, WindowBackdropType.Mica);
            else
                SystemThemeWatcher.UnWatch(w);
        }
    }

    public static bool TryParse(string? hex, out Color color)
    {
        color = default;
        if (string.IsNullOrEmpty(hex)) return false;
        try
        {
            color = (Color)ColorConverter.ConvertFromString(hex);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
