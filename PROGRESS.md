# PROGRESS

## Done phases
- Phase 1: skeleton + core timer
- Phase 2: autostart + startup settings
- Phase 3: full settings page
- Phase 4: tray + compact mode + taskbar progress
- Phase 5: statistics
- Phase 6: release build + README.md (Russian)
(User allowed proceeding to the next phase without waiting for "continue".)

## Decisions
- Project created directly in `PomodoroTimer/` (PomodoroTimer.csproj, net10.0-windows, WPF).
- Packages: CommunityToolkit.Mvvm 8.4.2, WPF-UI 4.3.0.
- Strings: `Properties/Strings.resx` + hand-written accessor `Properties/Strings.cs` (add a property there for each new resx key).
- `Core/PomodoroEngine.cs`: state machine; remaining time from `_endUtc` (UtcNow); `Tick()` called by DispatcherTimer (200 ms) and on PowerModes.Resume.
- On phase end the engine advances to the next phase and stops (auto-start toggles come in Phase 3).
- Session counter shows current work session in cycle (stays at N/N during long break).
- Single instance: Mutex + named EventWaitHandle (`Services/SingleInstanceGuard.cs`); second launch calls `MainWindow.BringToFront()`.
- Theme: currently follows system via `SystemThemeWatcher`; App.xaml defaults to Dark dictionary.

## Phase 2 notes (done)
- `Services/AppSettings.cs`: `AppSettings` (LaunchWithWindows=true, StartMinimized=false) + `SettingsService` (JSON in %APPDATA%\PomodoroTimer\settings.json; missing file = first run → defaults saved; corrupt file → defaults). Phase 3 should extend `AppSettings` and `SettingsService`, not replace them.
- `Services/AutostartService.cs`: HKCU Run value `PomodoroTimer` = quoted `Environment.ProcessPath`; `Sync(enabled)` is called on every launch (re-registers if exe moved); `IsDisabledByUser` reads StartupApproved\Run (first byte odd = disabled).
- `SettingsWindow` (modal FluentWindow, opened from "Настройки" button on main window): autostart toggle, startup mode combo, warning InfoBar refreshed on window activation. Phase 3 expands this window/page.
- "Свернуть в трей" currently just starts the window Minimized; real tray hiding comes in Phase 4.
- Note: any run of the Debug build registers the Debug exe path in HKCU Run (by design); removed manually after smoke test.

## Phase 3 notes (done)
- csproj TFM is now `net10.0-windows10.0.19041.0` (needed by Microsoft.Toolkit.Uwp.Notifications 7.1.3, only NuGet added in this phase).
- `Services/AppServices.cs` = composition root (Store, Autostart, Sound, Toast, Theme, Hotkeys, Engine). `SettingsService.Save()` raises `Changed` → `AppServices.Apply()` re-applies everything (timer options, autostart sync, theme/accent, hotkeys). Any new setting: add to `AppSettings` (+ `Sanitize`), add VM property in `SettingsViewModel` using `Set(...)`, handle in `Apply()` if needed.
- `PomodoroEngine.ApplyOptions()` keeps elapsed time and changes phase length live. Auto-start toggles live in `TimerOptions`; apply only on natural phase end (not Skip).
- Sounds: `SoundService` uses WPF MediaPlayer; built-in wavs are synthesized into %APPDATA%\PomodoroTimer\sounds\ on first use; events = WorkEnd, BreakEnd, plus tick (each second while Work is running, if enabled).
- Toast via `ToastContentBuilder` (best-effort, try/catch), shown on PhaseCompleted when enabled. Not visually verified.
- Theme: `ThemeService` (System/Light/Dark + 9 accent swatches, null = system accent); windows register via `Theme.Register(window)`; system watcher only active in System mode.
- Hotkeys: `HotkeyService` (RegisterHotKey on message-only HwndSource). Defaults Ctrl+Alt+P (toggle), Ctrl+Alt+N (skip). Verified Ctrl+Alt+P toggles the timer. Modifier required.
- Settings window is a scrollable modal FluentWindow; includes reset to defaults, export/import JSON (import is sanitized).
- `Properties/Strings.cs` is generated from the resx names by a one-liner script (see git-less history: parse resx `data@name`, emit `public static string X => Get(nameof(X));`).
- DailyGoal is stored but unused until Phase 5.

## Phase 4 notes (done)
- Tray: **H.NotifyIcon.Wpf 2.4.1** (explicitly allowed in the spec). WPF-UI.Tray 4.3.0 was tried first but its NotifyIcon never registered when created in code (IsRegistered stayed false) → package removed. `Services/TrayService.cs`: tooltip "<phase> — mm:ss" refreshed every second; menu Start/Pause (text follows state), Skip, Show window, Settings, Exit; left click shows window. Verified via HKCU\Control Panel\NotifyIconSettings entry that the icon registers.
- `Services/WindowCoordinator.cs` owns Main/Mini/Settings windows and one shared `MainViewModel`; `ShowMain/ShowMini/OpenSettings/Exit`. `IsExiting` lets the main window really close (tray Exit, session ending).
- New settings: `CloseToTray` (default true; combo in settings), `MiniLeft/MiniTop` (saved after drag and on close; clamped to virtual screen on restore).
- Close window → hidden to tray (verified process stays alive; second launch re-shows it). "Свернуть в трей" at startup = main window is never shown (verified).
- `MiniWindow`: borderless, Topmost, no taskbar entry, drag to move, double-click to return to main window; opened with "Мини-режим" button on main window (not in tray menu — not in spec).
- Taskbar progress: `TaskbarItemInfo` bound to `MainViewModel.TaskbarState/Progress` (Normal when running, Paused when paused mid-phase, None otherwise). Not visually verified.
- App icon generated by script: `Assets/app.ico` (tomato; 16/32/48/256 PNG frames), used as exe icon, window icon and tray icon.

## Phase 5 notes (done)
- `Services/HistoryService.cs`: `history.json` list of `{CompletedAt (local), Phase, DurationMinutes, Task}`; logs EVERY naturally completed phase (work and breaks; Skip/Reset never logged); stats count Work only. Corrupt file is renamed to `history.corrupt-<ts>.json`. CSV export: UTF-8 with BOM, columns Date,Time,Phase(Russian name),DurationMinutes,Task.
- `PomodoroEngine.PhaseCompleted` is now `Action<PomodoroPhase, TimeSpan>` (phase, its length).
- (Superseded by "Session names" below) Task label: `AppServices.TaskLabel` TextBox on the main window — removed.
- `StatsWindow`/`StatsViewModel` (non-modal, opened via "Статистика" button; live refresh on history/settings change): today / week (Mon–today) / month, goal progress, 7-day bar chart (XAML bars), CSV export.
- Verified end-to-end with a 1-min session: entry written with the label, stats showed 1 session / 1 min / goal 1 of 8. Test history file was moved out of %APPDATA% afterwards.
- JSON writers use UnsafeRelaxedJsonEscaping so Cyrillic stays readable.

## Phase 6 notes (done)
- csproj: `IncludeNativeLibrariesForSelfExtract` + `EnableCompressionInSingleFile` so the given publish command yields ONE exe (~71 MB; without these WPF native DLLs stayed next to it, 157 MB).
- Published exe tested from a different folder: window opens, Run entry points to that path. First start ~15 s (self-extract), next ~2 s. Self-extraction goes to %TEMP%\.net (runtime behavior, outside HKCU/%APPDATA%; unavoidable for WPF single-file).
- NOT verified (needs user): reboot autostart, clean Windows 11 PC, toast appearance, tray menu, taskbar progress, mini-window drag/position, sounds, CSV export, theme/accent switching visuals.

## Post-release tweak: richer tray tooltip
- `TrayService.UpdateTooltip`: 3 lines (+ optional task line): "<phase> — mm:ss (до HH:mm)" / "(пауза)" / plain when idle; "Сессия N/M · далее: <next phase> <duration>"; "Сегодня: done/goal · focus time". Task label appended only if it fits (native tooltip limit 127 chars, text is truncated with "…").
- Engine gained `NextPhase`, `NextPhaseDuration`, `EndTimeLocal`; `TimeFormat.Minutes` shared with stats.
- Tooltip text is refreshed every second; visual check on hover not done by Claude.

## Audit (stats / single instance / paths) — all OK
- Stats verified with seeded history (today 3, week Mon–today, month 7, goal 3/8, 7-day bars, breaks excluded, older entries excluded) and CSV export (BOM, rows, Russian phase names). Stats window now refreshes on activation (midnight rollover).
- Single instance: Mutex `Local\PomodoroTimer.SingleInstance` + event `Local\PomodoroTimer.Activate`; 4 launches of the published exe → 1 process.
- Paths: no relative paths anywhere. User data = `%APPDATA%\PomodoroTimer` (via Environment.GetFolderPath), icons = embedded pack:// resources, built-in sounds generated into %APPDATA%, custom sounds = absolute paths from the file dialog (missing file → built-in fallback). Deliberately NOT using AppContext.BaseDirectory for data: exe folder may be read-only, and single-file extraction dir is temporary. App tested launched with a different working directory.

## Session names ("Настроить таймеры") — replaces the task-label TextBox
- Data: `AppSettings.SessionNames` (`Services/SessionNames.cs`): AllWork / AllShortBreaks / LongBreak + per-session lists `Work[]`, `ShortBreaks[]` (index 0 = session 1). Single resolver: `SessionNames.Resolve(phase, session)` = per-session override → "for all" → null; UI fallback to the phase name via `PhaseNames.Display(settings, phase, session)`.
- Short break N belongs to `SessionInCycle == N` (it is the break after work N); the long break ignores the session number.
- Sanitize: trims, blank → null, names capped at 60 chars, lists capped at 20 entries (= max `LongBreakInterval`, so they are never shortened when the interval is lowered); old settings.json without the section and `"SessionNames": null` load fine.
- Engine: only additions — `NextSessionInCycle`, and `PhaseCompleted` now `Action<PomodoroPhase, int session, TimeSpan>` (session needed because the engine has already advanced when subscribers run). Timing and cycle order untouched.
- UI: `TimersWindow` + `TimersViewModel`/`SessionRowViewModel` (non-modal, opened by `WindowCoordinator.OpenTimers()` from the new main-window button; rows rebuilt only when `LongBreakInterval` changes, typing never rebuilds rows; "сейчас" badge on the current row, live via `Engine.Changed`). Names save on every keystroke.
- Shown in: main window (name = large title, subtitle "<фаза> · Сессия N/M", counter under the time hidden when named), mini window (`TitleText`, ellipsis), tray tooltip (current and "далее:" use names; the old task line was removed, 127-char truncation unchanged), toast "Далее: …".
- History: `AppServices.OnPhaseCompleted` stores the resolved name (null if none) in `HistoryEntry.Task` for ALL phases. `AppServices.TaskLabel` removed.
- `ThemeService.Apply` now returns early when theme+accent are unchanged (settings are saved per keystroke in the names dialog; avoids re-applying the theme every time).
- Verified (scratch console harness referencing the built dll, in the session scratchpad): cycle stepping with Skip for interval 4 (current and next names, `NextPhase/NextSessionInCycle` predictions match the real transitions), JSON round trip, old file / null section, interval 4→2→4 (also through JSON), caps.
- NOT yet verified in the running app: UI checks (main/mini window text while stepping with Skip, Timers window rows/"сейчас" badge, persistence after restart) and the 1-minute work + break history test. Reason: the user's own autostarted instance (published exe) was running and the single-instance mutex blocks a second copy; stopping it was not permitted. Tray tooltip cannot be read by UI automation at all, so it is checked only through the harness logic (same `PhaseNames.Display` calls).

## Notifications / window-on-top work (DONE: Steps 1–4)
- Step 2 (toast): `ToastService.Show(title, finishedName, nextText, offerStart)` — `ToastScenario.Reminder` (stays until acted on), silent (the app plays its own sound), buttons "Начать" (hidden when auto-start of the next phase is on) and "Показать окно"; body click = show window. Title "Работа окончена"/"Перерыв окончен", lines: finished session name, "Далее: <next name> · <N мин>". `ToastNotificationManagerCompat.OnActivated` (background thread) is marshalled to the UI dispatcher and exposed as `ToastService.ActionInvoked`; every toast carries a per-process `run` id, so a click on a toast of an earlier run (after restart) can only show the window, never start the timer. `AppServices` handles "start", `WindowCoordinator` handles "show". The existing `ToastEnabled` setting is now labelled "Уведомление Windows, которое остаётся до нажатия" (off = no toast at all).
- Step 3 (window at phase end): `AppServices.PhaseEnded` (only if `ShowWindowOnPhaseEnd`, default on) → `WindowCoordinator.OnPhaseEnded`: closes the mini window, sets temporary Topmost, shows the main window with `ShowActivated=false` (restores a minimized one with `SW_SHOWNOACTIVATE`), `FlashWindowEx` (FLASHW_ALL|FLASHW_TIMERNOFG) until the user switches to it. Temporary Topmost ends when the window is activated or after 10 s.
- Step 4: `AppSettings.AlwaysOnTop` (default false) + Settings section "Окно" (close action + toggle) + checkable tray item "Поверх всех окон"; tray item re-reads the setting each time the menu opens, the settings toggle follows changes via `Store.Changed`. ALL Topmost logic is in `WindowCoordinator.DesiredTopmost`/`ApplyTopmost()` (= AlwaysOnTop OR temporary alert mode), applied to the main window and the open Settings/Stats/Timers dialogs. `MainWindow.BringToFront` ends with `Topmost = DesiredTopmost` (no longer resets it). Mini window is always Topmost as before.
- New bool settings need no clamping in `Sanitize` (nothing to validate).
- Verified with the debug AND the published single-file exe (scripted: separate WinForms editor window instead of Notepad so the user's own windows are never touched, hotkey overridden to Ctrl+Alt+F9 in the test settings, everything restored afterwards): tray start and mini start → after the 1-min phase the main window appears with WS_EX_TOPMOST, the editor keeps keyboard focus and typing continues; 11 s later Topmost is gone (AlwaysOnTop off) and the editor can cover it, or it stays (on); AlwaysOnTop survives restart, close-to-tray + show, Settings/Stats/Timers open topmost and above main in both modes; Settings toggle switches main + dialogs live and writes settings.json; both notification switches off → only sound/history, no toast, no window.
- NOT verified (needs eyes): the actual toast banner and its buttons/click handling (also stale-toast click after restart), taskbar button blinking, the tray menu's checkable item (tray menu not reachable via UI automation).
- Note: the user's AutoHotkey script launches PomodoroTimer.exe on Ctrl+Alt+P, which collides with the app's default toggle hotkey on that setup (the user's own hotkey in settings.json is a different key).

### Step 1 (diagnosis)
- Step 1 DONE (diagnosis): `Services/AppLog.cs` (app.log, trimmed to 200 lines) + `ToastService` now logs the Windows `NotificationSetting` and any exception. Findings: no exception; setting = Enabled; same result from the Debug exe and the published single-file exe (both register an AUMID, `HasSentNotification=1`, per-app entry with `LastNotificationAddedTime`); global `ToastEnabled=1`, no per-app disable. No toast banner window appeared in UI Automation for ~20 s after the toast. So Windows accepts the toast and the banner is not (visibly) shown → most likely Do Not Disturb / automatic quiet moment (e.g. fullscreen app/video) or a short default banner (~5 s, non-persistent) that was missed. Not conclusively proven (reading the notification database was not permitted). User should check Win+N and Settings → System → Notifications. (Steps 2–4 were implemented afterwards, see above.)

## All phases done. Possible follow-ups (not requested): none pending.

## Phase 5 original plan (kept for reference)
Phase 5: statistics. history.json in %APPDATA%\PomodoroTimer (date, phase, duration, optional task label), task label TextBox on main window, Stats page (today/week/month sessions + focus minutes, daily goal progress using `AppSettings.DailyGoal`, 7-day bar chart, CSV export). Hook `PomodoroEngine.PhaseCompleted` (natural completions only; Skip doesn't raise it). Decide whether skipped work counts — currently no.

