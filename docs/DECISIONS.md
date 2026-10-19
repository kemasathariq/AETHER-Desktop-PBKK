# Technical decisions

Short records of choices made where the PRD or the references left room. Change a decision by editing its entry and the
code and tests it affects in the same change.

## D-01 .NET 8 target
Projects target `net8.0` (`net8.0-windows` for WPF). The PBKK lab names .NET 8 as the example framework. The development
Mac only has SDK 10; SDK 10 builds `net8.0` projects, and the test projects set `RollForward=Major` so they run on the .NET 10
runtime. .NET 8 reaches end of support in November 2026. Retargeting is a one-line change per csproj (`net10.0` /
`net10.0-windows`); the self-contained release (D-14) does not depend on an installed runtime.

## D-02 SQLite instead of the labs' SQL Server
The labs teach ADO.NET with SQL Server (`Microsoft.Data.SqlClient`). The PRD fixes SQLite with `Microsoft.Data.Sqlite`, so the PRD wins.
The same pattern is kept: repository classes, connection/command/reader, `using` for disposal, parameterized SQL, PK/FK/UNIQUE constraints, 1:N relation. No lab step
requires SQL Server for this project, so nothing conflicts.

## D-03 Source key is `serial_no`
`mto_no` is duplicated once in the source (`MTO-2026-OPP-0096`), so it cannot be a key. Imported incidents are keyed by
`SourceSerialNo` (unique); `Reference` stays an ordinary text column.

## D-04 Seed from an embedded JSON, imported once
`scripts/extract_seed.py` reads `references/aether_source/bundle.js` (never modified) and writes
`aether_seed.json`, embedded in `Aether.Infrastructure`. The app does not parse JavaScript. A `SeedLog` row prevents
re-import, so restarts do not duplicate data and user deletions stay deleted. Re-run the script only if the source data changes.

## D-05 Deleting an incident deletes its actions (PRD 11.1 open item)
PRD section 11.1 required a decision. Choice: after a confirmation dialog that states the number of actions, the incident and its actions are deleted in one
transaction (`ON DELETE CASCADE` plus an explicit transaction in the repository). Alternative (block deletion) was rejected because it
forces users to delete actions one by one for no benefit in a local single-user tool.

## D-06 Loss figures
The dashboard headline is **recorded actual loss** (`act_loss_k_usd`); potential loss is shown beside it and the total is derived (actual +
potential, which equals the source `total_loss_k_usd` on all rows). Money stays in k USD as in the source; no currency conversion. The
dashboard text states that these are historical records, not savings produced by the application.

## D-07 Incident priority rule
Deterministic scoring defined by the team (`Aether.Core/Services/PriorityService.cs`). It is **not** an algorithm of the original AETHER
case, not AI, and says nothing about root cause. Thresholds are round numbers chosen by the team; they are not statistics of the case data.

| Factor | Points |
|---|---|
| Recorded downtime | >= 24 h: 3; >= 8 h: 2; > 0 h: 1; 0 h: 0 |
| Recorded actual loss | >= 1,000 k USD: 3; >= 250: 2; > 0: 1; 0: 0 |
| Asset condition (only if the asset has weekly records dated in the 28 days up to the incident date **and** a source limit) | trip limit reached: 2; alarm limit reached: 1; otherwise 0 |

Category from the total: >= 6 Critical, 4-5 High, 2-3 Medium, 0-1 Low.

- **Missing data:** a null factor is "Not available", gets no points and is never treated as zero. If downtime or loss is missing the result is flagged
  "Partial data". If both are missing the incident is "Not rated". A missing condition factor is explained, not flagged (condition data exists for 5 of 380 assets).
- **Source Pre-Risk** is displayed as "source classification" and is **not** an input, in line with PRD section 13 (Pre-Risk must not be the only
  measure of consequence). Using it as a minor input was rejected to keep the explanation simple.
- **Condition check:** limit direction `high` breaches when value >= limit, `low` when value <= limit (limits and directions come from the source).
- Each result lists every factor with points, the recorded value used and a basis sentence. The same text appears in the UI.
- Tested in `PriorityServiceTests` (boundaries, missing data, windows, directions, determinism, Pre-Risk independence).

## D-08 Source fields not imported
`priority_score`/`priority_rank` (the prototype's own ranking; method not documented), `f_mechanism_corrected`, `month_year`, `fleet_summary`
(the app calculates its own), RCA narrative text, and `preventive` actions (no status). See `DATA_DICTIONARY.md`.

## D-09 Weekly condition only; hourly readings left out
Weekly condition data carries units (in headers), alarm/trip limits and Health Status, which is what PRD FR-07 asks for. The hourly PI series
(720 values x 7 tags x 5 assets) and its z-score "lead time" analysis are a separate analytic method from the case and would add a second chart model;
they are left out of the MVP. They are a possible extension that needs a new table and a new contract.

## D-10 Charts drawn natively
Bar, column and line charts are small WPF `FrameworkElement`s drawing with `DrawingContext` (`src/Aether.Desktop/Controls`). This avoids a third-party chart package, keeps
the dependency list to Microsoft.Data.Sqlite, and the line chart draws only recorded points (no smoothing or extrapolation). Every chart has a text or table equivalent on the page.

## D-11 Simple MVVM without a framework
`ObservableObject`, `RelayCommand`, one view model per page, view models created by hand in `MainViewModel`. No DI container or MVVM library. Repositories are
interfaces in `Aether.Core` so view models depend on contracts, not SQLite.

## D-12 Pages reload when opened
Each page calls `Refresh()` when selected, so dashboard statistics, priorities and pickers always reflect the database after edits elsewhere. The dashboard
query runs on a background task and shows a loading line.

## D-13 Numeric text input
Form numbers are text so bad input can be reported by field. Empty means not recorded. `12.5` and `12,5` are accepted; thousands separators, `NaN` and
`Infinity` are rejected (`NumberParser`, tested).

## D-14 Release: self-contained folder, not single file
Self-contained `win-x64` removes the need to install .NET. Single-file is supported by the script (`-SingleFile`, with
`IncludeNativeLibrariesForSelfExtract`) because `e_sqlite3.dll` is a native library, but it is **not the default**: it self-extracts native files at
first run and has not been tested on Windows. PRD section 14 says the option is fixed only after a package is tested on Windows; until then the plain folder is the documented path.
A cross-publish from macOS succeeds and contains `e_sqlite3.dll`; it has not been run on Windows.

## D-15 Origin column
`Incidents` and `ActionItems` carry `Origin` (`Source`/`User`) so recorded case data and user entries are never confused. Editing a source row keeps `Source`.
No illustrative data is created, so there is nothing to label as such.

## D-16 No teammate branches were available
The repository had no commits, branches or remotes when this work started, so Ferdian's and Brilian's modules were implemented here by the foundation
pass and documented as primary-owner areas in `TEAM_TASKS.md`. Nothing was merged from elsewhere. If they already have local work, follow the reconciliation
procedure in `TEAM_TASKS.md`.
