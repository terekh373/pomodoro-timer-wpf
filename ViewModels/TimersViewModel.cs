using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PomodoroTimer.Core;
using PomodoroTimer.Properties;
using PomodoroTimer.Services;

namespace PomodoroTimer.ViewModels;

/// <summary>One per-session name field (a work session or a short break).</summary>
public sealed class SessionRowViewModel : ObservableObject
{
    private readonly AppServices _svc;
    private readonly PomodoroPhase _phase;
    private readonly int _session;

    public SessionRowViewModel(AppServices svc, PomodoroPhase phase, int session)
    {
        _svc = svc;
        _phase = phase;
        _session = session;
        Label = string.Format(phase == PomodoroPhase.Work ? Strings.TimersWorkRowFormat : Strings.TimersShortRowFormat, session);
    }

    public string Label { get; }

    private SessionNames Names => _svc.Settings.SessionNames;

    public string Value
    {
        get => Names.GetOverride(_phase, _session) ?? "";
        set
        {
            Names.SetOverride(_phase, _session, value);
            _svc.Store.Save(); // applied immediately everywhere
            OnPropertyChanged();
        }
    }

    /// <summary>What applies when the field is empty: the "for all" value or the standard phase name.</summary>
    public string Placeholder
    {
        get
        {
            var all = _phase == PomodoroPhase.Work ? Names.AllWork : Names.AllShortBreaks;
            return string.IsNullOrWhiteSpace(all) ? PhaseNames.Get(_phase) : all.Trim();
        }
    }

    public bool IsCurrent => _svc.Engine.Phase == _phase && _svc.Engine.SessionInCycle == _session;

    public void RefreshInherited() => OnPropertyChanged(nameof(Placeholder));
    public void RefreshCurrent() => OnPropertyChanged(nameof(IsCurrent));
    public void RefreshAll() => OnPropertyChanged(string.Empty);
}

public sealed partial class TimersViewModel : ObservableObject, IDisposable
{
    private readonly AppServices _svc;
    private int _builtForInterval;

    public TimersViewModel(AppServices svc)
    {
        _svc = svc;
        svc.Store.Changed += OnStoreChanged;
        svc.Engine.Changed += OnEngineChanged;
        Rebuild();
    }

    public ObservableCollection<SessionRowViewModel> Rows { get; } = new();

    private SessionNames Names => _svc.Settings.SessionNames;

    public string AllWork
    {
        get => Names.AllWork ?? "";
        set { Names.AllWork = value; Commit(); }
    }

    public string AllShortBreaks
    {
        get => Names.AllShortBreaks ?? "";
        set { Names.AllShortBreaks = value; Commit(); }
    }

    public string LongBreak
    {
        get => Names.LongBreak ?? "";
        set { Names.LongBreak = value; Commit(); }
    }

    public string WorkPlaceholder => PhaseNames.Get(PomodoroPhase.Work);
    public string ShortBreakPlaceholder => PhaseNames.Get(PomodoroPhase.ShortBreak);
    public string LongBreakPlaceholder => PhaseNames.Get(PomodoroPhase.LongBreak);

    public bool IsLongBreakCurrent => _svc.Engine.Phase == PomodoroPhase.LongBreak;

    private void Commit([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
    {
        _svc.Store.Save();
        OnPropertyChanged(name);
    }

    [RelayCommand]
    private void ClearAll()
    {
        Names.Clear();
        _svc.Store.Save();
        OnPropertyChanged(string.Empty);
        foreach (var row in Rows) row.RefreshAll();
    }

    /// <summary>Rows in cycle order: Работа 1, Короткий перерыв 1, …, Работа N (the long break has its own field above).</summary>
    private void Rebuild()
    {
        var interval = _svc.Settings.LongBreakInterval;
        _builtForInterval = interval;
        Rows.Clear();
        for (var i = 1; i <= interval; i++)
        {
            Rows.Add(new SessionRowViewModel(_svc, PomodoroPhase.Work, i));
            if (i < interval) Rows.Add(new SessionRowViewModel(_svc, PomodoroPhase.ShortBreak, i));
        }
    }

    private void OnStoreChanged()
    {
        if (_svc.Settings.LongBreakInterval != _builtForInterval)
        {
            Rebuild();
            return;
        }

        // Only the inherited-value hints change; never touch Value, the user may be typing in a field.
        foreach (var row in Rows) row.RefreshInherited();
    }

    private void OnEngineChanged()
    {
        foreach (var row in Rows) row.RefreshCurrent();
        OnPropertyChanged(nameof(IsLongBreakCurrent));
    }

    public void Dispose()
    {
        _svc.Store.Changed -= OnStoreChanged;
        _svc.Engine.Changed -= OnEngineChanged;
    }
}
