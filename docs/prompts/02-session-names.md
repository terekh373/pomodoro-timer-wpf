# Prompt 2: session names

6 Nov 2026. I wanted to name sessions ("Работа" = "Программирование", first short break = "Застелить постель" and so on). Claude read the project first and wrote the prompt against the real classes. Executed by Claude Code.

---

You are working in an existing .NET 10 WPF app (WPF-UI, CommunityToolkit.Mvvm) in the current folder. Read PROGRESS.md first: it records the architecture and decisions. Deliver exactly the feature below; no other refactors or features.

## Goal
Replace the optional "Над чем работаем? (необязательно)" TextBox on the main window with a "Настроить таймеры" button that opens a dialog for naming sessions. Example: every "Работа" = "Программирование", short break #1 = "Застелить постель", short break #2 = "Разобрать вещи", work #3 = "Настройка шаблона".

## How the cycle works now (do not change)
- `Core/PomodoroEngine.cs`: Phase (Work/ShortBreak/LongBreak) and `SessionInCycle` (1..LongBreakInterval). Work N is followed by short break N, except after the last work session, which is followed by the long break. So one cycle = N work sessions, N-1 short breaks and 1 long break (N = `LongBreakInterval`, 1..20).
- `AppServices.TaskLabel` (not persisted) is saved into history only for Work entries. This feature replaces it.

## Data model (Services/AppSettings.cs, persisted in settings.json)
Add a `SessionNames` class property on AppSettings:
- `string? AllWork`, `string? AllShortBreaks`, `string? LongBreak` (the "for all" values)
- `List<string?> Work`: per-session overrides, index 0 = work session 1
- `List<string?> ShortBreaks`: index 0 = short break 1
Rules:
- Resolve the name in one place (a `SessionNameResolver` or a method on SessionNames): per-session override if not blank → "for all" value if not blank → null.
- Sanitize(): null → new(); trim; cap each name at 60 chars; never truncate the lists when LongBreakInterval decreases (the user's names must survive increasing it again). Old settings.json files without this section must load with no names.
- Export/import of settings already serializes AppSettings: confirm the new section round-trips.

## Dialog (new TimersWindow.xaml + TimersViewModel, same WPF-UI style as SettingsWindow, opened through WindowCoordinator like Settings/Stats)
Section "Для всех сессий":
- «Все сессии "Работа"»: TextBox
- «Все сессии "Короткий перерыв"»: TextBox
- «Сессия "Длинный перерыв"»: TextBox
Section "Отдельно для каждой сессии": rows generated in cycle order for the current LongBreakInterval (Работа 1, Короткий перерыв 1, Работа 2, … Работа N, Длинный перерыв). The long break appears only in the first section, so don't duplicate it here. Row labels use a format string such as «{0}-я сессия "Работа"» / «{0}-й короткий перерыв». Each row's placeholder shows the inherited value (the "for all" value or the standard phase name) so the user can see what applies when the field is empty. Mark the row of the current session.
Buttons: "Очистить все" (clears all names) and "Закрыть". Changes save automatically through SettingsService like the rest of the settings and apply immediately. Put the section in a ScrollViewer: with an interval of 20 there are 39 rows.

## Where the resolved name is shown
- Main window: if a name exists, show it large in place of `PhaseText`, with a smaller line below: "<phase name> · <session N/M>". Without a name, keep the current layout.
- MiniWindow, tray tooltip (TrayService: keep the 127-char limit and its truncation logic) and the toast "next" text: use the name when there is one, otherwise the phase name. In the tray tooltip, the "далее:" part shows the next session's name.
- History: on PhaseCompleted, store the resolved name in `HistoryEntry.Task` for ALL phases, breaks included (stats keep counting only Work, as now). Remove `AppServices.TaskLabel` and the TextBox, and update every place that used them.
All UI strings go into Properties/Strings.resx and Strings.cs, as in the rest of the project.

## Constraints
- Don't change the engine's timing logic or the cycle order.
- No new NuGet packages. Only change files inside this project. Stop and ask before deleting any file other than code you are replacing.

## Done when
- `dotnet build` has 0 errors and no new warnings.
- With interval 4: setting AllWork="Программирование", short break 1="Застелить постель", work 3="Настройка шаблона" shows the right name for every session when you step through the cycle with Skip (check main window, mini window and tray tooltip).
- Restarting the app keeps the names. An old settings.json without SessionNames loads without errors.
- Changing the interval 4 → 2 → 4 keeps the names for sessions 3–4.
- A completed 1-minute work session and a 1-minute break both write their names into history.json (use a temporary test setup and restore it afterwards, as in the Phase 5 audit).
- PROGRESS.md gets a short section on this feature, including anything you couldn't check visually.
Finish with a list of changed files and the results of the checks above.
