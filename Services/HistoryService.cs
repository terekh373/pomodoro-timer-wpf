using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PomodoroTimer.Core;
using PomodoroTimer.ViewModels;

namespace PomodoroTimer.Services;

public sealed class HistoryEntry
{
    /// <summary>Local time when the phase finished.</summary>
    public DateTime CompletedAt { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PomodoroPhase Phase { get; set; }

    public double DurationMinutes { get; set; }

    public string? Task { get; set; }
}

/// <summary>Completed phases log in %APPDATA%\PomodoroTimer\history.json.</summary>
public sealed class HistoryService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // keep Cyrillic readable
    };

    private readonly string _path = Path.Combine(SettingsService.DataDirectory, "history.json");
    private List<HistoryEntry> _entries = new();

    public IReadOnlyList<HistoryEntry> Entries => _entries;

    public event Action? Changed;

    public void Load()
    {
        if (!File.Exists(_path)) return;
        try
        {
            _entries = JsonSerializer.Deserialize<List<HistoryEntry>>(File.ReadAllText(_path), JsonOptions) ?? new();
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            // Keep the unreadable file for manual recovery instead of overwriting it.
            try { File.Move(_path, Path.ChangeExtension(_path, $".corrupt-{DateTime.Now:yyyyMMddHHmmss}.json")); }
            catch (IOException) { }
            _entries = new();
        }
    }

    public void Add(PomodoroPhase phase, TimeSpan duration, string? task)
    {
        _entries.Add(new HistoryEntry
        {
            CompletedAt = DateTime.Now,
            Phase = phase,
            DurationMinutes = Math.Round(duration.TotalMinutes, 2),
            Task = string.IsNullOrWhiteSpace(task) ? null : task.Trim(),
        });
        Save();
        Changed?.Invoke();
    }

    private void Save()
    {
        Directory.CreateDirectory(SettingsService.DataDirectory);
        var tmp = _path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(_entries, JsonOptions));
        File.Move(tmp, _path, overwrite: true);
    }

    public void ExportCsv(string path)
    {
        var sb = new StringBuilder("Date,Time,Phase,DurationMinutes,Task\r\n");
        foreach (var e in _entries.OrderBy(e => e.CompletedAt))
        {
            sb.Append(e.CompletedAt.ToString("yyyy-MM-dd")).Append(',')
              .Append(e.CompletedAt.ToString("HH:mm:ss")).Append(',')
              .Append(PhaseNames.Get(e.Phase)).Append(',')
              .Append(e.DurationMinutes.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(',')
              .Append(Csv(e.Task)).Append("\r\n");
        }
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)); // BOM for Excel
    }

    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.AsSpan().IndexOfAny(",\"\r\n") >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
    }
}
