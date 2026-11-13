using System.Runtime.InteropServices;

namespace PomodoroTimer.Services;

internal static class NativeMethods
{
    private const uint FlashAll = 3;          // caption + taskbar button
    private const uint FlashTimerNoFg = 0xC;  // keep flashing until the window comes to the foreground
    public const int SwShowNoActivate = 4;

    [StructLayout(LayoutKind.Sequential)]
    private struct FlashWInfo
    {
        public uint cbSize;
        public IntPtr hwnd;
        public uint dwFlags;
        public uint uCount;
        public uint dwTimeout;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FlashWindowEx(ref FlashWInfo pwfi);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    /// <summary>Flashes the taskbar button until the user switches to the window.</summary>
    public static void FlashUntilForeground(IntPtr hwnd)
    {
        var info = new FlashWInfo
        {
            cbSize = (uint)Marshal.SizeOf<FlashWInfo>(),
            hwnd = hwnd,
            dwFlags = FlashAll | FlashTimerNoFg,
            uCount = uint.MaxValue,
        };
        FlashWindowEx(ref info);
    }
}
