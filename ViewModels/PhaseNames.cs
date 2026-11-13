using PomodoroTimer.Core;
using PomodoroTimer.Properties;
using PomodoroTimer.Services;

namespace PomodoroTimer.ViewModels;

public static class PhaseNames
{
    public static string Get(PomodoroPhase phase) => phase switch
    {
        PomodoroPhase.Work => Strings.PhaseWork,
        PomodoroPhase.ShortBreak => Strings.PhaseShortBreak,
        _ => Strings.PhaseLongBreak,
    };

    /// <summary>The user's name for this session if there is one, otherwise the standard phase name.</summary>
    public static string Display(AppSettings settings, PomodoroPhase phase, int session) =>
        settings.SessionNames.Resolve(phase, session) ?? Get(phase);
}
