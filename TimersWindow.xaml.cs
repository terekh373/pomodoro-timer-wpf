using System.Windows;
using PomodoroTimer.Services;
using PomodoroTimer.ViewModels;
using Wpf.Ui.Controls;

namespace PomodoroTimer;

public partial class TimersWindow : FluentWindow
{
    public TimersWindow(AppServices services)
    {
        var vm = new TimersViewModel(services);
        DataContext = vm;
        InitializeComponent();
        services.Theme.Register(this);
        Closed += (_, _) => vm.Dispose();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
