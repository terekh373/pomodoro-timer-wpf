using PomodoroTimer.Properties;

namespace PomodoroTimer.ViewModels;

public static class TimeFormat
{
    /// <summary>mm:ss, rounded up so 00:00 only appears when the phase really ended.</summary>
    public static string Format(TimeSpan remaining)
    {
        var seconds = (int)Math.Ceiling(remaining.TotalSeconds);
        return $"{seconds / 60:00}:{seconds % 60:00}";
    }

    /// <summary>"45 мин" / "1 ч 15 мин".</summary>
    public static string Minutes(double minutes)
    {
        var total = (int)Math.Round(minutes);
        return total >= 60
            ? string.Format(Strings.HoursMinutesFormat, total / 60, total % 60)
            : string.Format(Strings.MinutesFormat, total);
    }
}
