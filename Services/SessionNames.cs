using PomodoroTimer.Core;

namespace PomodoroTimer.Services;

/// <summary>
/// User-defined names for sessions of the cycle. Resolution order for a session:
/// per-session override → "for all" value → null (caller falls back to the phase name).
/// </summary>
public sealed class SessionNames
{
    public const int MaxNameLength = 60;

    /// <summary>Longest supported cycle (AppSettings.LongBreakInterval maximum); lists never need more entries.</summary>
    public const int MaxSessions = 20;

    public string? AllWork { get; set; }
    public string? AllShortBreaks { get; set; }
    public string? LongBreak { get; set; }

    /// <summary>Per-session overrides; index 0 = work session 1.</summary>
    public List<string?> Work { get; set; } = new();

    /// <summary>Per-session overrides; index 0 = short break 1 (the break after work session 1).</summary>
    public List<string?> ShortBreaks { get; set; } = new();

    /// <param name="session">1-based <c>SessionInCycle</c> the phase belongs to (ignored for the long break).</param>
    /// <returns>The trimmed name, or null when no name applies.</returns>
    public string? Resolve(PomodoroPhase phase, int session) => phase switch
    {
        PomodoroPhase.Work => Pick(Work, session, AllWork),
        PomodoroPhase.ShortBreak => Pick(ShortBreaks, session, AllShortBreaks),
        _ => NonBlank(LongBreak),
    };

    /// <summary>Raw per-session override (null if none).</summary>
    public string? GetOverride(PomodoroPhase phase, int session)
    {
        var list = ListFor(phase);
        var i = session - 1;
        return i >= 0 && i < list.Count ? list[i] : null;
    }

    public void SetOverride(PomodoroPhase phase, int session, string? value)
    {
        var list = ListFor(phase);
        var i = session - 1;
        if (i < 0 || i >= MaxSessions) return;
        while (list.Count <= i) list.Add(null);
        list[i] = value;
    }

    public void Clear()
    {
        AllWork = AllShortBreaks = LongBreak = null;
        Work.Clear();
        ShortBreaks.Clear();
    }

    /// <summary>Fills nulls, trims and caps names. Lists are NOT shortened when the interval shrinks.</summary>
    public void Sanitize()
    {
        AllWork = Clean(AllWork);
        AllShortBreaks = Clean(AllShortBreaks);
        LongBreak = Clean(LongBreak);
        Work = CleanList(Work);
        ShortBreaks = CleanList(ShortBreaks);
    }

    private List<string?> ListFor(PomodoroPhase phase) => phase == PomodoroPhase.Work ? Work : ShortBreaks;

    private static string? Pick(List<string?>? list, int session, string? all)
    {
        var i = session - 1;
        var over = list != null && i >= 0 && i < list.Count ? list[i] : null;
        return NonBlank(over) ?? NonBlank(all);
    }

    private static string? NonBlank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static string? Clean(string? s)
    {
        var t = NonBlank(s);
        return t is { Length: > MaxNameLength } ? t[..MaxNameLength] : t;
    }

    private static List<string?> CleanList(List<string?>? list) =>
        (list ?? new List<string?>()).Take(MaxSessions).Select(Clean).ToList();
}
