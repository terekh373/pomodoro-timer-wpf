using System.Windows.Threading;
using Microsoft.Toolkit.Uwp.Notifications;
using PomodoroTimer.Properties;

namespace PomodoroTimer.Services;

/// <summary>
/// Windows toast shown at the end of a phase. It uses the Reminder scenario, so it stays on screen
/// until the user acts; its buttons / body click are reported through <see cref="ActionInvoked"/>.
/// </summary>
public sealed class ToastService
{
    public const string ActionStart = "start";
    public const string ActionShow = "show";

    private readonly string _runId = Guid.NewGuid().ToString("N");
    private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

    /// <summary>Raised on the UI thread with <see cref="ActionStart"/> or <see cref="ActionShow"/>.</summary>
    public event Action<string>? ActionInvoked;

    public ToastService()
    {
        // Must be subscribed early: a click on a toast of a previous run launches the exe and is delivered here.
        ToastNotificationManagerCompat.OnActivated += OnActivated;
    }

    public void Show(string title, string finishedName, string nextText, bool offerStart)
    {
        try
        {
            // NotificationSetting tells whether Windows will display toasts for this app at all.
            var setting = ToastNotificationManagerCompat.CreateToastNotifier().Setting;
            AppLog.Info($"Toast requested (Windows notification setting for this app: {setting}): \"{title}\" / \"{finishedName}\" / \"{nextText}\"");

            var builder = new ToastContentBuilder()
                .SetToastScenario(ToastScenario.Reminder) // stays until dismissed; requires at least one button
                .AddArgument("action", ActionShow)        // body click
                .AddArgument("run", _runId)
                .AddText(title)
                .AddText(finishedName)
                .AddText(nextText)
                .AddAudio(new ToastAudio { Silent = true }); // the app plays its own sound

            if (offerStart)
            {
                builder.AddButton(new ToastButton()
                    .SetContent(Strings.ToastStart)
                    .AddArgument("action", ActionStart)
                    .AddArgument("run", _runId));
            }

            builder.AddButton(new ToastButton()
                .SetContent(Strings.ToastShowWindow)
                .AddArgument("action", ActionShow)
                .AddArgument("run", _runId));

            builder.Show();
            AppLog.Info("Toast handed to Windows without exception");
        }
        catch (Exception ex)
        {
            AppLog.Error("Toast failed", ex);
        }
    }

    // Runs on a background thread.
    private void OnActivated(ToastNotificationActivatedEventArgsCompat e)
    {
        try
        {
            var args = ToastArguments.Parse(e.Argument);
            args.TryGetValue("action", out var action);
            args.TryGetValue("run", out var run);

            // A toast from a previous run must never start the timer: only bring the window up.
            if (action != ActionStart || run != _runId)
                action = ActionShow;

            AppLog.Info($"Toast activated: action={action}, fromThisRun={run == _runId}");
            _dispatcher.BeginInvoke(() => ActionInvoked?.Invoke(action!));
        }
        catch (Exception ex)
        {
            AppLog.Error("Toast activation failed", ex);
        }
    }
}
