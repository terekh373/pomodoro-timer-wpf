using Microsoft.Win32;

namespace PomodoroTimer.Services;

/// <summary>Manages the HKCU Run entry that starts the app at Windows login.</summary>
public sealed class AutostartService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ApprovedKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
    private const string ValueName = "PomodoroTimer";

    private static string Command => $"\"{Environment.ProcessPath}\"";

    public bool IsRegistered
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            return key?.GetValue(ValueName) is string;
        }
    }

    /// <summary>True when the Run entry points to the current exe path.</summary>
    public bool IsRegisteredForCurrentPath
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            return key?.GetValue(ValueName) is string s && string.Equals(s, Command, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// True when the user turned the app off in Windows Settings → Startup apps.
    /// StartupApproved\Run holds 12 bytes; an even first byte (02/06) means enabled, odd (03/07) disabled.
    /// </summary>
    public bool IsDisabledByUser
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(ApprovedKeyPath);
            return key?.GetValue(ValueName) is byte[] { Length: > 0 } data && (data[0] & 1) == 1;
        }
    }

    public void Enable()
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        key.SetValue(ValueName, Command, RegistryValueKind.String);
    }

    public void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    /// <summary>Makes the registry match the setting; re-registers if the exe was moved.</summary>
    public void Sync(bool enabled)
    {
        if (enabled)
        {
            if (!IsRegisteredForCurrentPath) Enable();
        }
        else if (IsRegistered)
        {
            Disable();
        }
    }
}
