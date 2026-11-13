using System.IO;

namespace PomodoroTimer.Services;

/// <summary>Tiny diagnostic log in %APPDATA%\PomodoroTimer\app.log, trimmed to the last lines. Never throws.</summary>
public static class AppLog
{
    private const int KeepLines = 200;
    private const long TrimAboveBytes = 64 * 1024;

    private static readonly object Gate = new();
    private static readonly string Path_ = Path.Combine(SettingsService.DataDirectory, "app.log");

    public static void Info(string message) => Write("INFO ", message);

    public static void Error(string context, Exception ex) =>
        Write("ERROR", $"{context}: {ex.GetType().FullName}: {ex.Message}");

    private static void Write(string level, string message)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(SettingsService.DataDirectory);
                File.AppendAllText(Path_, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {level} {message}{Environment.NewLine}");

                if (new FileInfo(Path_).Length > TrimAboveBytes)
                {
                    var lines = File.ReadAllLines(Path_);
                    if (lines.Length > KeepLines)
                        File.WriteAllLines(Path_, lines[^KeepLines..]);
                }
            }
        }
        catch
        {
            // Logging must never break the app.
        }
    }
}
