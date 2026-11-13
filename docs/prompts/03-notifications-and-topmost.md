# Prompt 3: noticeable phase end + "always on top"

8 Nov 2026. The end of a phase was easy to miss: only a quiet sound, and the toast never seemed to show up. The prompt makes the agent find out why before adding anything. Executed by Claude Code.

---

You are working in an existing .NET 10 WPF app (WPF-UI, H.NotifyIcon, Microsoft.Toolkit.Uwp.Notifications 7.1.3) in the current folder. Read PROGRESS.md first. Deliver only the features below.

## Problem
When a work session or break ends, the user hears only a quiet sound and easily keeps working or resting too long. A toast is supposed to show (`AppServices.OnPhaseCompleted` → `ToastService.Show`, `ToastEnabled` = true by default), but the user doesn't see one. `ToastService` swallows every exception with an empty `catch`, so the real cause is hidden.

## Step 1: Diagnose why the toast doesn't show (report before fixing)
- Replace the empty catch with logging to %APPDATA%\PomodoroTimer\app.log (timestamp + exception type + message; keep the file small, e.g. trim to the last 200 lines).
- Check the usual causes for an unpackaged single-file WPF app and report which applies: an exception from ToastNotificationManagerCompat, the app disabled in Windows Settings → System → Notifications, Do Not Disturb / Focus suppressing it (the toast goes silently to the notification center), toasts not working from the published single-file exe while working from `dotnet run`.
- Output "✅ Step 1: <cause>" and STOP. Wait for my "continue". If the cause is a Windows setting I must change myself, say exactly which one.

## Step 2: A noticeable toast at the end of a phase
- Title: "Работа окончена" / "Перерыв окончен" (existing strings). Text: the name of the finished session (via `Settings.SessionNames` / `PhaseNames.Display`, the session names feature is already implemented) and "Далее: <next session name> · <duration>".
- Use `ToastScenario.Reminder` so the toast stays on screen until the user acts (Reminder requires at least one button, so add them):
  - "Начать" → starts the next phase (`Engine.Start()`)
  - "Показать окно" → `WindowCoordinator.ShowMain()`
  - Clicking the toast body = "Показать окно".
- Handle `ToastNotificationManagerCompat.OnActivated`: it fires on a background thread, so marshal to the UI Dispatcher. Make sure a click on an old toast after the app restarted doesn't crash it or start a second instance (the existing SingleInstanceGuard must still hold).
- If auto-start for the next phase is on, the "Начать" button is hidden (the phase is already running).

## Step 3: Bring up the timer window at the end of a phase
- AppServices must not depend on windows. Add an event (e.g. `PhaseEnded`) that WindowCoordinator subscribes to.
- On phase end: if the main window is hidden in the tray, minimized or covered by other windows, show it over all other windows — the same result as "Показать окно" in the tray. If the mini window is open, expand to the main window as ShowMain already does.
- Do NOT steal keyboard focus: the user may be typing in another app. Show the window topmost without activating it (e.g. ShowActivated=false / Topmost toggle without Activate), then call FlashWindowEx on the taskbar button until the user switches to the window. Topmost is temporary: switch it off when the user clicks the window, or after 10 seconds — UNLESS the permanent "Поверх всех окон" setting from Step 4 is on, in which case it stays on.

## Step 4: "Поверх всех окон" (Topmost) toggle
- New setting `AlwaysOnTop` (bool, default false) in AppSettings, persisted and handled in Sanitize, string in Strings.resx.
- In Settings, "Окно" section (next to the "close to tray" option): a ToggleSwitch "Поверх всех окон". It applies immediately, without restarting.
- When on, the main window stays above other windows permanently, including after it's hidden to the tray and shown again (ShowMain/BringToFront must not reset it), and after an app restart.
- Owned dialogs (Settings, Stats, Timers) must still open above the main window and must not end up behind it.
- Also add a "Поверх всех окон" checkable item to the tray menu, synced with the setting both ways.
- The mini window stays always on top as now, regardless of this setting.
- All Topmost logic lives in one place (e.g. a method in WindowCoordinator such as `ApplyTopmost()`), used by both Step 3 and Step 4, so the temporary and permanent modes don't conflict.

## Step 2–3 settings
In Settings, "Уведомления" section, add two separate checkboxes (defaults on, persisted in AppSettings with Sanitize, strings in Strings.resx):
- "Показывать окно таймера по окончании этапа"
- "Уведомление Windows, которое остаётся до нажатия" (applies to the existing ToastEnabled; if it's off, no toast at all)

## Constraints
- Don't change the timer logic, the cycle order or the session names.
- No new NuGet packages. P/Invoke (FlashWindowEx) is allowed. Only change files inside this project. Stop and ask before deleting any file.

## Done when
- `dotnet build`: 0 errors, no new warnings.
- With work = 1 min and short break = 1 min, run both from the tray (main window hidden) and from mini mode: the window appears over a Notepad window while the cursor keeps typing in Notepad; the taskbar button blinks; the toast stays until clicked; "Начать" and "Показать окно" work.
- "Поверх всех окон" off: 10 seconds after a phase ends (or after clicking the window), the window can be covered by Notepad again. On: the window stays above Notepad at all times, after hide-to-tray → "Показать окно", and after an app restart. The toggle in Settings and the tray menu item stay in sync. Settings/Stats/Timers open above the main window in both modes.
- The same checks from the published single-file exe (publish command in README.md).
- With both notification checkboxes off: only the sound plays, as before.
- PROGRESS.md: a short section about the features, what Step 1 found and what you couldn't check visually.
Finish with a list of changed files and the result of each check.
