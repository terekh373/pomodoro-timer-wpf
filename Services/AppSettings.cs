using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PomodoroTimer.Services;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AppThemeMode { System, Light, Dark }

public sealed class SoundEventSettings
{
    public bool Enabled { get; set; } = true;

    /// <summary>Custom .wav/.mp3 path; null/empty = built-in sound.</summary>
    public string? FilePath { get; set; }
}

/// <summary>Global hotkey: Win32 modifier flags (Alt=1, Ctrl=2, Shift=4, Win=8) + WPF <c>Key</c> value (0 = none).</summary>
public sealed class HotkeySettings
{
    public uint Modifiers { get; set; }
    public int Key { get; set; }

    public HotkeySettings() { }
    public HotkeySettings(uint modifiers, int key) { Modifiers = modifiers; Key = key; }
}

public sealed class AppSettings
{
    public const int MinMinutes = 1;
    public const int MaxMinutes = 180;

    // Startup
    public bool LaunchWithWindows { get; set; } = true;
    public bool StartMinimized { get; set; }

    // Timer
    public int WorkMinutes { get; set; } = 25;
    public int ShortBreakMinutes { get; set; } = 5;
    public int LongBreakMinutes { get; set; } = 15;
    public int LongBreakInterval { get; set; } = 4;
    public bool AutoStartBreak { get; set; }
    public bool AutoStartWork { get; set; }
    public int DailyGoal { get; set; } = 8;

    // Sounds
    public SoundEventSettings WorkEndSound { get; set; } = new();
    public SoundEventSettings BreakEndSound { get; set; } = new();
    public int Volume { get; set; } = 70;
    public bool TickingEnabled { get; set; }

    // Notifications
    /// <summary>Windows toast at the end of a phase (Reminder: stays until the user acts).</summary>
    public bool ToastEnabled { get; set; } = true;

    /// <summary>Bring the timer window up (without taking focus) when a phase ends.</summary>
    public bool ShowWindowOnPhaseEnd { get; set; } = true;

    // Appearance
    public AppThemeMode Theme { get; set; } = AppThemeMode.System;

    /// <summary>Accent color as #RRGGBB; null/empty = system accent.</summary>
    public string? AccentColor { get; set; }

    // Names of the sessions in the cycle (optional)
    public SessionNames SessionNames { get; set; } = new();

    // Window behavior
    /// <summary>True: closing the window hides it to the tray; false: closing exits the app.</summary>
    public bool CloseToTray { get; set; } = true;

    /// <summary>Keep the main window above all other windows (the mini window is always on top anyway).</summary>
    public bool AlwaysOnTop { get; set; }

    /// <summary>Last position of the compact window (null = not placed yet).</summary>
    public double? MiniLeft { get; set; }
    public double? MiniTop { get; set; }

    // Hotkeys (Ctrl+Alt+P / Ctrl+Alt+N; key 0 = disabled)
    public HotkeySettings ToggleHotkey { get; set; } = new(3, (int)System.Windows.Input.Key.P);
    public HotkeySettings SkipHotkey { get; set; } = new(3, (int)System.Windows.Input.Key.N);

    /// <summary>Clamps values and fills nulls; used after loading/importing untrusted JSON.</summary>
    public void Sanitize()
    {
        WorkMinutes = Math.Clamp(WorkMinutes, MinMinutes, MaxMinutes);
        ShortBreakMinutes = Math.Clamp(ShortBreakMinutes, MinMinutes, MaxMinutes);
        LongBreakMinutes = Math.Clamp(LongBreakMinutes, MinMinutes, MaxMinutes);
        LongBreakInterval = Math.Clamp(LongBreakInterval, 1, 20);
        DailyGoal = Math.Clamp(DailyGoal, 1, 100);
        Volume = Math.Clamp(Volume, 0, 100);
        WorkEndSound ??= new();
        BreakEndSound ??= new();
        ToggleHotkey ??= new();
        SkipHotkey ??= new();
        SessionNames ??= new();
        SessionNames.Sanitize();
        if (!Enum.IsDefined(Theme)) Theme = AppThemeMode.System;
        if (!string.IsNullOrEmpty(AccentColor) && !System.Text.RegularExpressions.Regex.IsMatch(AccentColor, "^#[0-9A-Fa-f]{6}$"))
            AccentColor = null;
    }
}

/// <summary>Loads/saves settings as JSON in %APPDATA%\PomodoroTimer\settings.json.</summary>
public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // keep Cyrillic paths readable
    };

    private readonly string _path;

    public AppSettings Settings { get; private set; } = new();

    /// <summary>True when no settings file existed (defaults were used and saved).</summary>
    public bool IsFirstRun { get; private set; }

    /// <summary>Raised after every save; subscribers re-apply the settings.</summary>
    public event Action? Changed;

    public static string DataDirectory { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PomodoroTimer");

    public SettingsService()
    {
        _path = Path.Combine(DataDirectory, "settings.json");
    }

    public void Load()
    {
        IsFirstRun = !File.Exists(_path);
        if (!IsFirstRun)
        {
            try
            {
                Settings = Deserialize(File.ReadAllText(_path));
            }
            catch (Exception ex) when (ex is JsonException or IOException)
            {
                Settings = new AppSettings(); // corrupted file: fall back to defaults
            }
        }
        else
        {
            Save();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(DataDirectory);
        File.WriteAllText(_path, Serialize(Settings));
        Changed?.Invoke();
    }

    /// <summary>Replaces all settings (reset to defaults / import) and saves.</summary>
    public void Replace(AppSettings settings)
    {
        settings.Sanitize();
        Settings = settings;
        Save();
    }

    public void ExportTo(string path) => File.WriteAllText(path, Serialize(Settings));

    /// <exception cref="JsonException">The file is not a valid settings file.</exception>
    public static AppSettings ImportFrom(string path) => Deserialize(File.ReadAllText(path));

    private static string Serialize(AppSettings s) => JsonSerializer.Serialize(s, JsonOptions);

    private static AppSettings Deserialize(string json)
    {
        var s = JsonSerializer.Deserialize<AppSettings>(json) ?? throw new JsonException("empty");
        s.Sanitize();
        return s;
    }
}
