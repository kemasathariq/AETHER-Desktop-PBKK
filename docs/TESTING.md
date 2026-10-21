# Testing

## Verified on Windows

21 October 2026: Windows x64 (OS build 26200), .NET SDK 10.0.402, target/runtime .NET 8. All 85 tests passed with no skipped tests. Restore completed with NuGet audit enabled. No database server is needed.

```powershell
.\scripts\build.ps1
# or
dotnet test Aether.sln -c Release -m:1
```

| Project | Tests | Coverage |
|---|---:|---|
| Aether.Core.Tests | 54 | Required fields, negative/nonfinite numbers, null versus zero, number parsing, derived loss, action statuses, priority boundaries, missing data, condition windows and limit directions, unknown units/parameters, Pre-Risk independence |
| Aether.Infrastructure.Tests | 28 | Schema and constraints, import counts/totals and repeat startup, incident/action CRUD, persistence on reopening, search and combined filters, SQL injection text, cascade delete, measurements and units, dashboard queries and empty data |
| Aether.Desktop.Tests | 3 | Selection notification, saving under active filters, and a Windows acceptance scenario using actual WPF views, commands, validators, and SQLite repositories |

The WPF acceptance scenario checks all five pages at 1360x820 and 1100x680, reports binding errors, preserves declared table column widths, creates/edits incidents and actions, rejects blank/invalid/negative input, accepts decimal commas, checks filtering, dashboard refresh, persistence after reopening, all five assets and their parameters, KO-3201 priority factors, and empty view models. It executes commands and bindings in process; it does not inject physical mouse or keyboard input.

Screenshots are written to `tests/Aether.Desktop.Tests/bin/Release/net8.0-windows/Screenshots/`. Dashboard, incident list/details, actions/form, asset information/chart, and priority/details were visually inspected, including the minimum layouts. Wide tables use horizontal scrolling. Temporary database files isolate these checks from the user's database.

On macOS/Linux, run `scripts/check.sh`: it compiles the solution and runs only Core and Infrastructure tests. WPF acceptance tests need a Windows desktop session.

## Published release check

```powershell
.\scripts\publish.ps1
.\scripts\verify-release.ps1
```

The default self-contained Windows x64 folder release passed both launches on this Windows machine. The check verifies a main window, a nonempty SQLite file, normal exit, restart, and that the process loads `coreclr.dll` from the release folder. It uses a unique database under the temporary directory and retains it for inspection. It checks the presence of WPF and native SQLite dependencies.

`publish.ps1` also creates `artifacts/AetherDesktop-win-x64.zip`, including the Indonesian user guide. Extract the entire ZIP before opening `AetherDesktop.exe`. The verifier applies to folder releases. Optional `-SingleFile` publishing remains untested.

## Acceptance evidence and remaining manual checks

Automated evidence below is from the Windows run. The last column describes additional manual acceptance; it is not a claim those interactions have been performed.

| ID | Criterion | Automated evidence | Additional manual check |
|---|---|---|---|
| AC-01 | All pages open | All actual page templates render without binding errors at normal/minimum sizes | Click every sidebar item; navigate with arrow keys |
| AC-02 | Statistics/charts follow data | Repository aggregates; dashboard starts at 380 incidents; form save increases count to 381 and actions to 21 | Check charts after adding/deleting a record |
| AC-03 | Incident CRUD | Form commands create/edit; repository tests cover delete/cascade; selection panel regression checked | Click Delete and check both Cancel and confirmation |
| AC-04 | Search/filter | Query combinations; WPF search with no match and clear filters | Enter KO-3201 and combine plant/status controls |
| AC-05 | Only available asset data | Five assets, 26 weekly records per parameter, units/limits, and rendered charts | Scroll measurements and change parameters with keyboard |
| AC-06 | Explainable priority | Boundaries/missing values; KO-3201 Critical with factors displayed; no unit/parameter means not scored | Expand rules and inspect partial-data examples |
| AC-07 | Linked action CRUD | Form commands create/edit status; repository relation/delete/cascade tests | Exercise delete confirmation and incident dropdown |
| AC-08 | Persistence | Reopened SQLite retains incident/action changes; release starts twice with same test file | Close/reopen after making changes through controls |
| AC-09 | Invalid input rejected | Empty forms, abc, negative downtime, parser, repository validation | Type invalid dates into DatePicker |
| AC-10 | Tests run | 85 passed on Windows x64 | Follow README on a teammate's machine |
| AC-11 | Run/build instructions | Build, publish and verifier executed; Indonesian guide included | A teammate follows the guide independently |
| AC-12 | Standalone release | EXE starts/restarts with bundled .NET and local SQLite | Repeat on second Windows PC without .NET |

Still unverified: physical keyboard/mouse workflows, delete confirmation interactions, invalid DatePicker text, focus visibility on a real display, 125%/150% DPI scaling, Windows 10, a second clean PC, and optional single-file publishing. Computer Use could not inspect the launched window in this session (minimized-window/activation errors), so no manual interaction is recorded as passed.
