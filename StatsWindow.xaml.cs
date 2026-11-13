using PomodoroTimer.Services;
using PomodoroTimer.ViewModels;
using Wpf.Ui.Controls;

namespace PomodoroTimer;

public partial class StatsWindow : FluentWindow
{
    public StatsWindow(AppServices services)
    {
        var vm = new StatsViewModel(services);
        DataContext = vm;
        InitializeComponent();
        services.Theme.Register(this);
        Activated += (_, _) => vm.Refresh();
        Closed += (_, _) => vm.Dispose();
    }
}
