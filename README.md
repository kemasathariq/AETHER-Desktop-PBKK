# AETHER Desktop

Windows desktop application (WPF, C#, SQLite) for reviewing manufacturing incident data and following up on actions. Built for the PBKK midterm project from the
approved PRD (`docs/PRD.md`) and the CALIBER 2026 Case 2 data (`references/`).

## 1. Purpose and features

Five screens in one window with a sidebar:

| Screen | What it does |
|---|---|
| Dashboard | Total incidents, recorded downtime and recorded loss, incidents by status/plant/month, latest incidents. All numbers are database queries. |
| Incidents | Search and filter table; create, view, edit, delete with validation and delete confirmation. |
| Asset Monitoring | The 5 assets that have weekly condition data in the case data: units, dates, alarm/trip limits, history chart. |
| Incident Priority | A documented, rule-based priority (not AI) with the factors, points and recorded values behind each result; the source Pre-Risk class is shown separately. |
| Action Tracker | Create, view, edit, delete actions linked to incidents (owner, target date, Open / In Progress / Closed). |

What it is not: no login, no live DCS/historian/CMMS link, no AI or root-cause identification, no savings or ROI claims. The figures are historical case records, not
results produced by this app. Details of the rule and decisions: `docs/DECISIONS.md`.

## 2. Prerequisites

| Task | Needs |
|---|---|
| Run or build the app | Windows 10/11 x64, **.NET SDK 8.0 or newer** (`dotnet --list-sdks`), Git. Visual Studio 2022 ".NET desktop development" is optional. |
| Run the published release | Windows 10/11 x64 only (the release is self-contained) |
| Run all tests | Windows with .NET SDK 8+; WPF acceptance tests require a Windows desktop session |
| Core / Infrastructure tests | Any OS with .NET SDK 8+ (macOS/Linux can compile WPF but cannot run its tests) |

No SQL Server or other database server is needed. Packages are restored from nuget.org.

## 3. Repository structure

```
Aether.sln
Directory.Build.props          shared build settings
src/
  Aether.Core/                 models, validation, repository interfaces, PriorityService
  Aether.Infrastructure/       SQLite schema, repositories, case-data import (Seed/aether_seed.json)
  Aether.Desktop/              WPF app (Views, ViewModels, Controls, Themes, Common)
tests/
  Aether.Core.Tests/           validation and prioritization tests
  Aether.Infrastructure.Tests/ database, import and repository tests
  Aether.Desktop.Tests/        Windows WPF rendering, form and persistence acceptance tests
docs/                          PRD, architecture, design, data dictionary, database, testing, decisions, demo, team tasks, contributing
scripts/                       build.ps1, publish.ps1, verify-release.ps1, reset-database.ps1, check.sh, extract_seed.py
references/                    original PRD, case data, labs, deck (read-only input)
```

## 4. Restore, build, run

From the repository root, in PowerShell or a terminal:

```
dotnet restore Aether.sln
dotnet build Aether.sln -c Release -m:1
dotnet run --project src/Aether.Desktop -c Release
```

In Visual Studio open `Aether.sln`, set `Aether.Desktop` as the startup project and press F5. On first start the database is created and the case data imported (a second or two).

## 5. Run the tests

```
dotnet test Aether.sln -c Release -m:1
```

or `scripts\build.ps1` (restore + build + test, Windows) / `scripts/check.sh` (macOS/Linux, Core and Infrastructure tests). 85 tests on Windows: 54 Core, 28 Infrastructure, 3 Desktop acceptance tests.
Persistence tests use temporary database files. Details and the manual Windows checklist: `docs/TESTING.md`.

## 6. Database

- Location: `%LOCALAPPDATA%\AetherDesktop\aether.db` (outside the install folder). Set the environment variable `AETHER_DB_PATH` to use another file.
- First run: the file and tables are created, then the case data is imported once: 380 incidents, 5 assets, 520 weekly condition values, 20 RCA actions. Restarts never duplicate data.
- Everything you add or change is stored in this file and survives restarts.
- Reset to clean case data: close the app, run `scripts\reset-database.ps1` (moves the file to a timestamped backup), start the app again.
- Full detail, schema and field mapping: `docs/DATABASE.md`, `docs/DATA_DICTIONARY.md`.

## 7. Build and publish a Windows x64 release

```
scripts\publish.ps1
```

which runs:

```
dotnet publish src/Aether.Desktop/Aether.Desktop.csproj -c Release -r win-x64 --self-contained true -o artifacts/AetherDesktop-win-x64
```

The script creates `artifacts\AetherDesktop-win-x64.zip` with the executable, dependencies, and an Indonesian user guide (`PANDUAN.txt`). Extract the whole ZIP and open `AetherDesktop.exe`; keep the supporting files beside it. The package needs no .NET installation or database server.

Run `scripts\verify-release.ps1` to check startup, normal exit, restart, database creation, and use of the bundled runtime with a separate test database. This passed on Windows x64 on 21 October 2026. The release has not been checked on a second clean PC; single-file publishing (`-SingleFile`) remains optional and unverified.

User instructions: [Panduan penggunaan](docs/PANDUAN.md). Detailed evidence and remaining manual checks: [Testing](docs/TESTING.md).

## 8. Environment limitations and troubleshooting

| Problem | What to do |
|---|---|
| WPF tests do not run on macOS/Linux | Use `scripts/check.sh` for Core/Infrastructure; run the full suite in a Windows desktop session. |
| `dotnet build` on Linux/macOS fails on the Desktop project | The repo sets `EnableWindowsTargeting`; use SDK 8 or newer and run `dotnet restore` first. |
| "The database could not be opened" at start | Check you can write to `%LOCALAPPDATA%\AetherDesktop`; or set `AETHER_DB_PATH` to a writable path. A message tells which file was used. |
| Data looks wrong or you want the original data | Run `scripts\reset-database.ps1` (backs up the old file). |
| Sidebar icons show squares | The Segoe MDL2 Assets font is missing; change the glyph code points in `ViewModels/MainViewModel.cs`. |
| Windows SmartScreen warns about the published exe | The exe is unsigned. Choose "More info" > "Run anyway" for demos. |
| .NET 8 support ends November 2026 | Change `net8.0`/`net8.0-windows` to `net10.0`/`net10.0-windows` in the four csproj files if your SDK allows; see `docs/DECISIONS.md` D-01. |

## 9. Team and contributing

| Person | Primary ownership |
|---|---|
| Kemas Muhammad Athariq | Foundation, architecture, documentation, shared contracts, integration, release |
| Ferdian Ardra Hafizhan | WPF interface, navigation, dashboard, charts, user interactions (`src/Aether.Desktop`) |
| Brilian Kurniawan Prasisto | Data mapping, SQLite, repositories, prioritization, business logic (`src/Aether.Infrastructure`, `Core/Validation`, `Core/Services`, `tests`) |

Ownership is primary responsibility, not exclusive access. Start with `docs/TEAM_TASKS.md` (per-person tasks, acceptance criteria, merge procedure) and `docs/CONTRIBUTING.md` (branches, code rules,
how to change a shared contract).

## 10. Implementation status

| Area | Status |
|---|---|
| Dashboard, Incidents, Asset Monitoring, Incident Priority, Action Tracker | Implemented. |
| SQLite schema, import, repositories, validation, prioritization | Implemented; Core/Infrastructure tests pass on Windows. |
| Windows WPF checks | 85 tests pass in total. Actual views rendered at 1360x820 and 1100x680; forms, filtering, dashboard refresh and persistence checked. |
| Windows release verification (AC-12) | Self-contained EXE starts, creates SQLite database, closes normally and restarts using its bundled runtime. |
| Windows acceptance checklist (AC-01 to AC-12) | Automated checks recorded in `docs/TESTING.md`; physical keyboard/mouse, DPI scaling, confirmation dialogs, and a second clean PC still need manual acceptance. |
| Hourly production readings (case data) | Deliberately not included (`docs/DECISIONS.md` D-09). |
| Teammate branches | None existed in the repository; nothing was merged. See `docs/TEAM_TASKS.md`. |

## References

`docs/PRD.md` (requirements), `docs/ARCHITECTURE.md`, `docs/DESIGN.md`, `docs/DATA_DICTIONARY.md`, `docs/DATABASE.md`, `docs/TESTING.md`, `docs/DECISIONS.md`, `docs/DEMO_SCRIPT.md`, `docs/TEAM_TASKS.md`, `docs/CONTRIBUTING.md`.
