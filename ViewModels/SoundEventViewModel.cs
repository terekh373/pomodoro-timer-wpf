using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PomodoroTimer.Properties;
using PomodoroTimer.Services;

namespace PomodoroTimer.ViewModels;

/// <summary>One row of the sound settings (enable switch + custom file) for a single event.</summary>
public sealed partial class SoundEventViewModel : ObservableObject
{
    private readonly AppServices _svc;
    private readonly SoundEvent _event;

    public SoundEventViewModel(AppServices svc, SoundEvent ev, string title)
    {
        _svc = svc;
        _event = ev;
        Title = title;
    }

    public string Title { get; }

    private SoundEventSettings Cfg =>
        _event == SoundEvent.WorkEnd ? _svc.Settings.WorkEndSound : _svc.Settings.BreakEndSound;

    public bool Enabled
    {
        get => Cfg.Enabled;
        set
        {
            if (Cfg.Enabled == value) return;
            Cfg.Enabled = value;
            _svc.Store.Save();
            OnPropertyChanged();
        }
    }

    public string FileName =>
        string.IsNullOrWhiteSpace(Cfg.FilePath) ? Strings.SoundDefault : Path.GetFileName(Cfg.FilePath);

    /// <summary>Re-read everything (after reset/import).</summary>
    public void Refresh() => OnPropertyChanged(string.Empty);

    [RelayCommand]
    private void Browse()
    {
        var dlg = new OpenFileDialog { Filter = Strings.SoundFilter };
        if (dlg.ShowDialog() != true) return;
        Cfg.FilePath = dlg.FileName;
        _svc.Store.Save();
        Refresh();
    }

    [RelayCommand]
    private void ClearFile()
    {
        Cfg.FilePath = null;
        _svc.Store.Save();
        Refresh();
    }

    [RelayCommand]
    private void Test() => _svc.Sound.Play(_event, ignoreEnabled: true);
}
