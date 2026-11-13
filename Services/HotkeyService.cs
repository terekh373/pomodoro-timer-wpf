using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace PomodoroTimer.Services;

/// <summary>Global hotkeys via RegisterHotKey on a hidden message-only window.</summary>
public sealed class HotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const uint ModNoRepeat = 0x4000;
    private static readonly IntPtr HwndMessage = new(-3);

    private readonly HwndSource _source;
    private readonly Dictionary<int, Action> _actions = new();
    private readonly Dictionary<int, HotkeySettings> _registered = new();

    public HotkeyService()
    {
        _source = new HwndSource(new HwndSourceParameters("PomodoroTimerHotkeys") { ParentWindow = HwndMessage });
        _source.AddHook(WndProc);
    }

    /// <summary>True if the hotkey is active (or intentionally empty).</summary>
    public bool IsActive(int id, HotkeySettings hk) => hk.Key == 0 || _registered.ContainsKey(id);

    /// <returns>False if Windows refused the combination (e.g. used by another app).</returns>
    public bool Register(int id, HotkeySettings hk, Action action)
    {
        _actions[id] = action;

        if (_registered.TryGetValue(id, out var current) &&
            current.Key == hk.Key && current.Modifiers == hk.Modifiers)
            return true;

        UnregisterHotKey(_source.Handle, id);
        _registered.Remove(id);
        if (hk.Key == 0) return true;

        var vk = (uint)KeyInterop.VirtualKeyFromKey((Key)hk.Key);
        if (!RegisterHotKey(_source.Handle, id, hk.Modifiers | ModNoRepeat, vk)) return false;

        _registered[id] = new HotkeySettings(hk.Modifiers, hk.Key);
        return true;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && _actions.TryGetValue(wParam.ToInt32(), out var action))
        {
            action();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        foreach (var id in _registered.Keys) UnregisterHotKey(_source.Handle, id);
        _source.Dispose();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
