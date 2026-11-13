# Pomodoro timer for Windows 11

A customizable Pomodoro timer: .NET 10, WPF, MVVM (CommunityToolkit.Mvvm), WPF-UI. It starts with Windows and opens its window, because I kept forgetting to launch [my previous WinForms timer](https://github.com/terekh373/MyPomodorroTimer).

The code was written by Claude Code from specs I prepared with Claude. The prompts are in [`docs/prompts`](docs/prompts), and [`PROGRESS.md`](PROGRESS.md) is the agent's own log of phases, decisions and checks.

What's inside: work / short / long break cycle, named sessions, sounds, Windows notifications that stay until clicked, the window coming up when a phase ends, tray icon and mini mode, global hotkeys, light / dark theme, statistics with CSV export. Settings and history live in `%APPDATA%\PomodoroTimer`.

Build: `dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true` (.NET 10 SDK).

---

# Помодоро-таймер для Windows 11

Настраиваемый таймер Pomodoro на WPF (.NET 10). Главное: **запускается вместе с Windows и сам открывает окно**.

Возможности: циклы работа / короткий и длинный перерыв, звуки и тиканье, уведомления Windows, светлая/тёмная/системная тема и акцентный цвет, глобальные горячие клавиши, значок в трее, мини-окно поверх всех окон, прогресс на панели задач, статистика и экспорт в CSV.

## Как собрать

Нужен [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

Обычная сборка для разработки:

```powershell
dotnet build
```

Один exe-файл для распространения (не требует установленного .NET):

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Готовый файл: `bin\Release\net10.0-windows10.0.19041.0\win-x64\publish\PomodoroTimer.exe` (около 70 МБ).
Его можно скопировать в любую постоянную папку (например, `C:\Users\<вы>\Apps\PomodoroTimer`) и запустить.

Замечания:

- Первый запуск exe занимает дольше (распаковка встроенных библиотек, до ~15 секунд), следующие — около 2 секунд.
- Не запускайте и не публикуйте программу из временной папки: путь к exe записывается в автозапуск. Если вы переместите exe, при следующем запуске путь обновится сам.

## Где хранятся данные

Всё лежит в `%APPDATA%\PomodoroTimer\` (то есть `C:\Users\<вы>\AppData\Roaming\PomodoroTimer\`):

| Файл | Содержимое |
|---|---|
| `settings.json` | все настройки |
| `history.json` | журнал завершённых фаз (дата, фаза, длительность, название сессии) |
| `sounds\` | встроенные звуки (создаются автоматически) |

Настройки можно экспортировать и импортировать на странице «Настройки» («Экспорт…» / «Импорт…»), там же есть «Сбросить к умолчаниям». История экспортируется в CSV из окна «Статистика».

Кроме этого программа пишет только в реестр текущего пользователя (`HKCU`): запись автозапуска и служебные записи для уведомлений Windows.

## Как отключить автозапуск

Любым из способов:

1. В программе: **Настройки → «Запускать вместе с Windows»** — выключить. Запись из реестра удаляется сразу.
2. В Windows: **Параметры → Приложения → Автозагрузка** — выключить «Помодоро». В этом случае в настройках программы появится предупреждение, что автозапуск отключён системой.
3. Вручную: удалить значение `PomodoroTimer` в ключе реестра `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run`.

Если вы не хотите, чтобы при входе открывалось окно, оставьте автозапуск включённым и выберите **«При запуске: Свернуть в трей»**.

## Горячие клавиши по умолчанию

- `Ctrl+Alt+P` — Старт / Пауза
- `Ctrl+Alt+N` — Пропустить фазу

Меняются в настройках (клик в поле и нажатие нужного сочетания, `Esc` — очистить).

## Полное удаление

Выключите автозапуск (см. выше), закройте программу через меню значка в трее («Выход»), удалите exe и папку `%APPDATA%\PomodoroTimer`.
