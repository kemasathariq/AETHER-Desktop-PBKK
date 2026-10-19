# Architecture

Layered solution, four projects plus two test projects. Dependencies point inward: `Desktop` -> `Infrastructure` -> `Core`; `Desktop` also uses `Core` types.

```
src/
  Aether.Core            models, validation, repository interfaces, PriorityService   (net8.0, no dependencies)
  Aether.Infrastructure  SQLite schema, repositories, case-data import                  (net8.0, Microsoft.Data.Sqlite)
  Aether.Desktop         WPF app: views, view models, charts, styles                     (net8.0-windows)
tests/
  Aether.Core.Tests            validation, number parsing, prioritization
  Aether.Infrastructure.Tests  database init, import, repositories, dashboard (temporary SQLite files)
```

## Aether.Core

| Area | Files |
|---|---|
| Models | `Incident`, `Asset`, `ConditionRecord`, `ActionItem`, `DashboardStats`, `PriorityResult`, `IncidentStatuses`, `ActionStatus`, `DataOrigin` |
| Validation | `IncidentValidator`, `ActionValidator`, `ValidationResult`, `NumberParser` |
| Contracts | `IIncidentRepository`, `IActionRepository`, `IAssetRepository`, `IDashboardRepository`, `IncidentFilter`, `ActionFilter` |
| Rules | `PriorityService` (static, pure) |
| Errors | `AetherException`, `ValidationException`, `NotFoundException` |

No UI or database types. Everything here is covered by unit tests that run on any OS.

## Aether.Infrastructure

`AetherDatabase` owns path, schema and connection creation (foreign keys on, pooling off). `Sqlite*Repository` classes implement the Core interfaces with
parameterized SQL and short-lived connections. `SourceDataImporter` loads the embedded `aether_seed.json`. `SqlHelpers.Run` converts `SqliteException` into
`AetherException` with a readable message. Details: `DATABASE.md`.

## Aether.Desktop

| Folder | Role |
|---|---|
| `Common` | `AppServices` (creates database + repositories at startup), `ObservableObject`, `RelayCommand`, `PageViewModel` (status message, `TryRun`), `FormErrors`, converters, `Dialogs` |
| `ViewModels` | `MainViewModel` (navigation) and one view model per page; `IncidentForm`/`ActionForm` hold editable text |
| `Views` | One `UserControl` per page plus `StatusMessage`; `MainWindow` maps view model types to views with `DataTemplate`s |
| `Controls` | `BarChart`, `ColumnChart`, `LineChart` |
| `Themes/Styles.xaml` | palette, typography, buttons, tables, focus visuals |

Flow of a change: view (binding/command) -> view model (parse form, validate with Core validators) -> repository interface -> SQLite -> view model reloads the list and
shows a success or error message. Other pages pick the change up when opened (`Refresh()`).

## Contracts that must stay compatible

These are the team's seams; see `TEAM_TASKS.md` for how to change them.

1. The four repository interfaces and filter classes (`Core/Repositories`).
2. The models and their nullable semantics (null = not recorded).
3. `DashboardStats` (producer: `SqliteDashboardRepository`; consumer: `DashboardViewModel`).
4. `PriorityService.Evaluate(Incident, IReadOnlyList<ConditionRecord>?) -> PriorityResult` and the `PriorityFactor` fields the UI shows.
5. Status constants (`IncidentStatuses`, `ActionStatus`) and the matching `CHECK` constraints in the schema.
6. Error convention: repositories validate and throw `ValidationException`/`NotFoundException`; other database failures become `AetherException`. View models catch `AetherException`
   (base type) and show its message; anything else is an unexpected bug caught by the global handler in `App.xaml.cs`.

## Platform notes

WPF only runs on Windows. On macOS/Linux the solution still restores and builds (`EnableWindowsTargeting` in `Directory.Build.props`) and the Core and
Infrastructure tests run; launching the app needs Windows 10/11 x64.
