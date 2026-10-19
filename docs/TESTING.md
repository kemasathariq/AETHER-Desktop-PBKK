# Testing

## Automated tests

```
dotnet test Aether.sln -c Release          # all tests
dotnet test tests/Aether.Core.Tests
dotnet test tests/Aether.Infrastructure.Tests
```

| Project | Tests | Covers |
|---|---|---|
| `Aether.Core.Tests` (51) | `ValidationTests`, `PriorityServiceTests` | required fields, negative/NaN/infinite numbers, null = not recorded, derived total loss, action validation and statuses, number parsing (`12.5`, `12,5`, rejects `1,584.5`, `NaN`), priority points and category boundaries, missing downtime/loss, "not rated", Pre-Risk has no effect, condition window and limit directions, determinism |
| `Aether.Infrastructure.Tests` (28) | `DatabaseAndSeedTests`, `RepositoryTests` | folder/file/table creation and repeat init, foreign keys enforced, import counts (380/5/520/20) and totals, import run twice, deleted rows stay deleted after restart, duplicate MTO number, malformed import data, persistence after reopening the file, incident and action CRUD, validation before save, not-found errors, SQL injection text stored as text, search/filter (case-insensitive, `%` literal, combined filters), action filters and relations, cascade delete of only that incident's actions, asset records with units/limits, empty database for assets and dashboard, dashboard sums/missing counts/follow-the-data/distributions/recent list, priority of the seeded KO-3201 incident from stored condition records |

Persistence tests use one temporary SQLite file each and delete it afterwards; they never touch `%LOCALAPPDATA%`.

Last run (macOS arm64, .NET SDK 10.0.401, tests rolled forward to the .NET 10 runtime): Core 51 passed, Infrastructure 28 passed, 0 failed, 0 skipped.
Run `scripts/check.sh` (macOS/Linux) or `scripts\build.ps1` (Windows) to repeat.

### Not covered by automated tests

The WPF layer (view models, XAML, charts, dialogs) has no automated tests: the project targets Windows and cannot run on the development Mac. Logic that decides results lives in Core and
Infrastructure on purpose so that it is tested. The Windows checks below cover the rest.

## Windows acceptance checklist (to run by hand)

Run on Windows 10/11 x64 with a fresh database (`scripts\reset-database.ps1`). Record Pass/Fail and the date next to each line.
Status column: **Automated** = the data behaviour behind it is covered by a passing test; **Windows** = needs the checks in the last column.

| ID | Criterion | Status today | Windows check |
|---|---|---|---|
| AC-01 | All pages open | Windows | Click each sidebar item; each page loads without an error dialog. Use arrow keys in the sidebar too. |
| AC-02 | Statistics and charts follow the database | Automated (aggregates) + Windows | Dashboard shows 380 incidents, 2,261.1 h, k USD 61,886 actual loss on first start. Add an incident with 10 h and k USD 100 actual loss, return to Dashboard: 381, 2,271.1 h, k USD 61,986. Delete it: back to the first numbers. |
| AC-03 | Incident CRUD | Automated (repository) + Windows | New incident with required fields -> appears in the table; Edit -> change saved; Delete -> confirmation, row gone. |
| AC-04 | Search and filter | Automated (queries) + Windows | Search `KO-3201` finds 1 row; `zzzz` shows "No incidents match..."; status and plant filters narrow the list; Clear filters restores it. |
| AC-05 | Asset page shows only available data | Automated + Windows | Exactly 5 assets listed, 26 weekly values per parameter, units and dates shown, no value for any other tag. Banner states coverage. The empty-assets message cannot be reached with the seeded database; it is only covered at repository level (empty database returns no assets). |
| AC-06 | Priority has inspectable reasons | Automated (rule) + Windows | Open Incident Priority, select KO-3201 (29 Apr 2026): Critical, factors downtime 3/3, loss 3/3, condition 2/2 (trip). Add an incident and leave Downtime empty (the source data has a downtime value on every incident): the downtime factor shows "Not scored" and the result is flagged "Partial data". |
| AC-07 | Actions managed and linked | Automated + Windows | Action Tracker shows 20 imported actions; KO-3201 has 4 (1 Closed, 1 In Progress, 2 Open). Add, edit status, delete an action. Delete an incident that has actions: dialog names the count, actions go with it. |
| AC-08 | Data survives restart | Automated (reopen file) + Windows | Add an incident and an action, close the app, reopen: both present, imported data not duplicated (still 380 + yours). |
| AC-09 | Invalid input rejected | Automated (validators) + Windows | Save empty form: red messages under required fields, nothing saved. Type `abc` or `-5` in Downtime: error. |
| AC-10 | Tests run | **Passed on macOS** | `dotnet test Aether.sln`; repeat on Windows. |
| AC-11 | README explains run/build | Windows | A teammate follows README on a clean Windows machine. |
| AC-12 | Release runs without a database server | Windows | `scripts\publish.ps1`, copy `artifacts\AetherDesktop-win-x64` to a machine without .NET or SQL Server, run `AetherDesktop.exe`, confirm the database appears under `%LOCALAPPDATA%\AetherDesktop` and the dashboard loads. |

Other Windows-only things to look at: sidebar icons render (Segoe MDL2 glyphs), amber focus ring on buttons/list/table with Tab, window at 1100x680 shows no clipped controls,
DatePicker works with keyboard, the three charts draw and the line chart marks alarm/trip lines, text contrast looks right on a laptop display and at 125%/150% scaling.

## Known gaps

- No test ever launched the WPF application. XAML compiles (BAML generation succeeded on macOS) but runtime binding errors would only appear on Windows (check the Visual Studio Output window for `System.Windows.Data Error`).
- Windows release not run on Windows (see AC-12).
