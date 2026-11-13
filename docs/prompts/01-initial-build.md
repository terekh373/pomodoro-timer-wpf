# Prompt 1: initial build (phases 1–6)

Written on 19 Oct 2026 in a Claude chat with the prompt-master skill, from my request in Russian: a customizable Pomodoro timer for Windows 11 that must start with Windows. Executed by Claude Code.

---

You are a senior C#/.NET desktop developer building a Windows 11 app. Work in phases. After EVERY phase: run `dotnet build`, fix all errors, update PROGRESS.md, output "✅ Phase N: <what was done>", then STOP and wait for my "continue".

## Goal
A fully customizable Pomodoro timer for Windows 11 that MUST launch automatically at Windows login and open its main window (I always forgot to start my previous WinForms timer, so autostart is the core requirement).

## Stack (fixed, do not change)
- .NET 10 (LTS), WPF, C#, MVVM via CommunityToolkit.Mvvm
- UI: WPF-UI (lepoco/wpfui), latest stable: Fluent design, Mica backdrop, light/dark/system theme
- Tray icon: the WPF-UI tray component or H.NotifyIcon.Wpf
- Toast notifications: Microsoft.Toolkit.Uwp.Notifications (or CommunityToolkit.WinUI.Notifications)
- Settings: JSON in %APPDATA%\PomodoroTimer\settings.json (System.Text.Json)
- No other NuGet packages without asking me first.

## Project rules
- Project folder: ./PomodoroTimer. Only create/edit files inside it.
- Keep PROGRESS.md in the root: done phases, decisions made, next step. Read it first at the start of each phase (the model serving you may change between sessions, so this file is your memory).
- UI language: Russian. All UI strings in one resource file (Strings.resx) so they can be changed later.
- Timer accuracy: compute remaining time from a stored target end DateTime, never by decrementing a counter. Handle PC sleep/resume correctly.
- Single instance: a second launch brings the existing window to front.

## Phase 1: Skeleton + core timer
- Main window: large remaining time, current phase (Работа / Короткий перерыв / Длинный перерыв), session counter (e.g. 2/4), buttons Start/Pause, Reset, Skip.
- Phase cycle: work → short break, every N-th work session → long break.
- Defaults: work 25, short 5, long 15 min, long break after 4 sessions.

## Phase 2: Autostart (critical)
- Register in HKCU\Software\Microsoft\Windows\CurrentVersion\Run with the current exe path (quoted). Enabled by default on first run.
- Settings toggle "Запускать вместе с Windows" (on/off) and "При запуске: открыть окно / свернуть в трей" (default: open window).
- If the user disabled the app in Windows Settings → Startup apps (HKCU\...\Explorer\StartupApproved\Run), detect it and show a warning in settings.
- Re-register the path on each launch if the exe was moved.

## Phase 3: Settings page (everything customizable)
- Durations of all 3 phases (1–180 min), long-break interval.
- Auto-start next break / next work session (separate toggles).
- Sounds: on/off per event, volume, custom .wav/.mp3 file per event, optional ticking sound during work.
- Toast notification on phase end (on/off).
- Theme: light/dark/system + accent color.
- Daily goal (number of sessions).
- Global hotkeys (configurable) for Start/Pause and Skip.
- Buttons: reset to defaults, export/import settings to a JSON file.
- Changes apply immediately and are saved automatically.

## Phase 4: Tray + compact mode
- Tray icon: tooltip with remaining time; menu: Start/Pause, Skip, Show window, Settings, Exit.
- Closing the window minimizes to tray (configurable: minimize or exit).
- Compact mini mode: small always-on-top draggable window showing time + phase, remembers position.
- Taskbar progress bar shows phase progress.

## Phase 5: Statistics
- Log every completed session to %APPDATA%\PomodoroTimer\history.json (date, phase, duration, optional task label).
- Optional task label text field on the main window.
- Stats page: today / week / month sessions and focus minutes, daily goal progress, simple bar chart for the last 7 days, export to CSV.

## Phase 6: Release build
- `dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true`
- README.md in Russian: how to build, where settings live, how to disable autostart.

## Forbidden
- No features beyond this list, no accounts, no cloud, no telemetry, no web servers.
- Do not touch the system outside HKCU and %APPDATA%.
- Stop and ask before: deleting any file, adding any NuGet package not listed above, changing the stack.

## Done when
- `dotnet build` has 0 errors and 0 warnings related to my code.
- After reboot the app starts and opens its window automatically.
- All settings persist after restart.
- The published single exe runs on a clean Windows 11 PC.
