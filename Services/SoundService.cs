using System.IO;
using System.Windows.Media;

namespace PomodoroTimer.Services;

public enum SoundEvent { WorkEnd, BreakEnd }

/// <summary>Plays event/tick sounds (custom file or built-in generated wav) at the configured volume.</summary>
public sealed class SoundService
{
    private readonly SettingsService _store;
    private readonly string _soundsDir = Path.Combine(SettingsService.DataDirectory, "sounds");
    private readonly HashSet<MediaPlayer> _active = new();
    private readonly MediaPlayer _tickPlayer = new();
    private bool _tickOpened;

    public SoundService(SettingsService store)
    {
        _store = store;
    }

    /// <param name="ignoreEnabled">True for the "test" button in settings.</param>
    public void Play(SoundEvent ev, bool ignoreEnabled = false)
    {
        var s = _store.Settings;
        var cfg = ev == SoundEvent.WorkEnd ? s.WorkEndSound : s.BreakEndSound;
        if (!cfg.Enabled && !ignoreEnabled) return;

        var builtIn = ev == SoundEvent.WorkEnd ? "work-end.wav" : "break-end.wav";
        var path = !string.IsNullOrWhiteSpace(cfg.FilePath) && File.Exists(cfg.FilePath)
            ? cfg.FilePath
            : EnsureBuiltIn(builtIn);

        var player = new MediaPlayer { Volume = s.Volume / 100.0 };
        _active.Add(player); // keep a reference until playback ends
        void Release(object? _, EventArgs __)
        {
            player.Close();
            _active.Remove(player);
        }
        player.MediaEnded += Release;
        player.MediaFailed += (o, _) => Release(o, EventArgs.Empty);
        player.Open(new Uri(path, UriKind.Absolute));
        player.Play();
    }

    public void PlayTick()
    {
        if (!_tickOpened)
        {
            _tickPlayer.Open(new Uri(EnsureBuiltIn("tick.wav"), UriKind.Absolute));
            _tickOpened = true;
        }
        _tickPlayer.Volume = _store.Settings.Volume / 100.0 * 0.6;
        _tickPlayer.Stop();
        _tickPlayer.Position = TimeSpan.Zero;
        _tickPlayer.Play();
    }

    private string EnsureBuiltIn(string name)
    {
        var path = Path.Combine(_soundsDir, name);
        if (File.Exists(path)) return path;

        Directory.CreateDirectory(_soundsDir);
        var data = name switch
        {
            "work-end.wav" => Wav.Notes(new[] { 784.0, 988.0, 1175.0 }, 0.22),
            "break-end.wav" => Wav.Notes(new[] { 880.0, 660.0 }, 0.28),
            _ => Wav.Notes(new[] { 1500.0 }, 0.03, decay: 120),
        };
        File.WriteAllBytes(path, data);
        return path;
    }

    /// <summary>Tiny 16-bit mono PCM wav synthesizer for the built-in sounds.</summary>
    private static class Wav
    {
        private const int Rate = 44100;

        public static byte[] Notes(double[] freqs, double noteSeconds, double decay = 9)
        {
            var perNote = (int)(Rate * noteSeconds);
            var samples = new short[perNote * freqs.Length];
            for (var n = 0; n < freqs.Length; n++)
            for (var i = 0; i < perNote; i++)
            {
                var t = (double)i / Rate;
                var v = Math.Sin(2 * Math.PI * freqs[n] * t) * Math.Exp(-decay * t) * 0.6;
                samples[n * perNote + i] = (short)(v * short.MaxValue);
            }

            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            var dataLen = samples.Length * 2;
            w.Write("RIFF"u8.ToArray());
            w.Write(36 + dataLen);
            w.Write("WAVEfmt "u8.ToArray());
            w.Write(16);
            w.Write((short)1);      // PCM
            w.Write((short)1);      // mono
            w.Write(Rate);
            w.Write(Rate * 2);      // byte rate
            w.Write((short)2);      // block align
            w.Write((short)16);     // bits
            w.Write("data"u8.ToArray());
            w.Write(dataLen);
            foreach (var s in samples) w.Write(s);
            return ms.ToArray();
        }
    }
}
