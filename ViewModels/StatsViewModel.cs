using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PomodoroTimer.Core;
using PomodoroTimer.Properties;
using PomodoroTimer.Services;

namespace PomodoroTimer.ViewModels;

public sealed record DayBar(string Label, int Count, double Height, string Tooltip);

public sealed partial class StatsViewModel : ObservableObject, IDisposable
{
    private const double MaxBarHeight = 120;
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

    private readonly AppServices _svc;

    [ObservableProperty] private string _todayText = "";
    [ObservableProperty] private string _weekText = "";
    [ObservableProperty] private string _monthText = "";
    [ObservableProperty] private string _goalText = "";
    [ObservableProperty] private double _goalProgress;
    [ObservableProperty] private IReadOnlyList<DayBar> _bars = Array.Empty<DayBar>();

    public StatsViewModel(AppServices svc)
    {
        _svc = svc;
        svc.History.Changed += Refresh;
        svc.Store.Changed += Refresh; // daily goal may change
        Refresh();
    }

    public void Dispose()
    {
        _svc.History.Changed -= Refresh;
        _svc.Store.Changed -= Refresh;
    }

    /// <summary>Recalculate everything; also called on window activation so "today" stays right after midnight.</summary>
    public void Refresh()
    {
        var work = _svc.History.Entries.Where(e => e.Phase == PomodoroPhase.Work).ToList();
        var today = DateTime.Today;
        var weekStart = today.AddDays(-(((int)today.DayOfWeek + 6) % 7)); // Monday
        var monthStart = new DateTime(today.Year, today.Month, 1);

        TodayText = Summary(work, today);
        WeekText = Summary(work, weekStart);
        MonthText = Summary(work, monthStart);

        var goal = _svc.Settings.DailyGoal;
        var done = work.Count(e => e.CompletedAt.Date == today);
        GoalText = string.Format(Strings.StatsGoalFormat, done, goal);
        GoalProgress = Math.Min(1.0, (double)done / goal);

        var days = Enumerable.Range(0, 7).Select(i => today.AddDays(i - 6)).ToList();
        var counts = days.Select(d => work.Count(e => e.CompletedAt.Date == d)).ToList();
        var max = Math.Max(1, counts.Max());
        Bars = days.Select((d, i) =>
        {
            var minutes = work.Where(e => e.CompletedAt.Date == d).Sum(e => e.DurationMinutes);
            var height = counts[i] == 0 ? 2 : Math.Max(6, counts[i] / (double)max * MaxBarHeight);
            return new DayBar(d.ToString("ddd", Ru), counts[i], height,
                string.Format(Strings.BarTooltipFormat, d.ToString("d MMMM", Ru), counts[i], FormatMinutes(minutes)));
        }).ToList();
    }

    private static string Summary(List<HistoryEntry> work, DateTime from)
    {
        var items = work.Where(e => e.CompletedAt >= from).ToList();
        return string.Format(Strings.StatsSummaryFormat, items.Count, FormatMinutes(items.Sum(e => e.DurationMinutes)));
    }

    private static string FormatMinutes(double minutes) => TimeFormat.Minutes(minutes);

    [RelayCommand]
    private void ExportCsv()
    {
        var dlg = new SaveFileDialog { Filter = Strings.CsvFilter, FileName = "pomodoro-history.csv" };
        if (dlg.ShowDialog() != true) return;
        try { _svc.History.ExportCsv(dlg.FileName); }
        catch (IOException ex) { System.Windows.MessageBox.Show(ex.Message, Strings.Stats); }
    }
}
