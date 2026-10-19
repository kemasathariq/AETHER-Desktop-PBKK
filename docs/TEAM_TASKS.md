# Team tasks and handoff

Read `ARCHITECTURE.md` first (10 minutes), then your section here. You should not need the project owner to explain the structure again.

**Current state:** all five screens, persistence, import, business rules, tests and documentation exist and build. The foundation pass wrote all three modules because no teammate branch
existed in the repository (`DECISIONS.md` D-16). The WPF app has **never been run on Windows**, so each owner's first job is to review, run and harden their own area, not to start from scratch.
Contracts below are frozen unless changed through the procedure in `CONTRIBUTING.md`.

## Ownership at a glance

| Person | Area | Primary files (you review and approve changes here) |
|---|---|---|
| Kemas Muhammad Athariq | Foundation, architecture, contracts, docs, integration, release | `Aether.sln`, `Directory.Build.props`, `docs/**`, `scripts/**`, `README.md`, `src/Aether.Core/Models/**`, `src/Aether.Core/Repositories/**`, `src/Aether.Core/AetherException.cs`, `src/Aether.Desktop/App.xaml*`, `Common/AppServices.cs`, `MainWindow.xaml*`, `ViewModels/MainViewModel.cs` |
| Ferdian Ardra Hafizhan | WPF interface, navigation, dashboard, charts, interactions | `src/Aether.Desktop/Views/**`, `ViewModels/*` (except `MainViewModel`), `Controls/**`, `Themes/**`, `Common/` (except `AppServices`) |
| Brilian Kurniawan Prasisto | Data mapping, SQLite, repositories, prioritization, business logic | `src/Aether.Infrastructure/**`, `src/Aether.Core/Validation/**`, `src/Aether.Core/Services/**`, `scripts/extract_seed.py`, `tests/**` |

Ownership means primary say, not exclusive access. Editing a file you do not own: tell the owner, keep their conventions, and keep the change small.

## Frozen contracts (do not break silently)

Repository interfaces and filters (`Core/Repositories`), models and null semantics (`Core/Models`), `DashboardStats`, `PriorityService.Evaluate` and `PriorityResult`, status constants vs. schema `CHECK`s,
error convention (`ValidationException`, `NotFoundException`, `AetherException`). Full list: `ARCHITECTURE.md`.

---

## Kemas: foundation, docs, integration, release

**Already done:** solution and project structure, shared models and interfaces, decisions log, all documents, scripts, release workflow proven as a cross-publish from macOS.

**To do (in this order):**
1. On Windows run `scripts\build.ps1`; confirm restore/build/test pass. Note SDK version in `TESTING.md`.
2. Run `scripts\publish.ps1`, copy the folder to a machine without .NET, run `AetherDesktop.exe` (AC-12). Record the result in `TESTING.md` and decide single-file or not (D-14).
3. Walk the checklist in `TESTING.md` with Ferdian; record Pass/Fail.
4. Merge teammates' branches (procedure below), keep `README.md` status section true.
5. Update `README.md` "Current implementation status" and `PRD.md` section 17 when something changes.

**Depends on:** Ferdian and Brilian finishing their reviews.
**Acceptance:** `dotnet build` and `dotnet test` pass on Windows; AC-11 and AC-12 pass; no doc describes behaviour the code lacks.

## Ferdian: WPF interface

**Already done:** sidebar shell, five views, view models, three native chart controls, styles, validation messages, confirmation dialogs, empty/loading/success/error states. Written without running on Windows.

**To do:**
1. Run the app on Windows (`dotnet run --project src/Aether.Desktop`). Open the Visual Studio Output window and fix every `System.Windows.Data Error` (binding paths, converters).
2. Verify visuals: Segoe MDL2 sidebar glyphs (`MainViewModel`), amber keyboard focus ring everywhere, no clipping at 1100x680, 125%/150% display scaling, DatePicker keyboard use, chart rendering and labels, contrast.
3. Walk each screen with an empty form, a validation error, a failed save, and an empty result list.
4. Fix layout or usability problems in `Views/**`, `Controls/**`, `Themes/**`. Keep logic out of XAML code-behind; put it in the view model.
5. Optional (only if time): highlight overdue actions (target date before today and status not Closed). Needs a derived property, not a schema change.

**Depends on:** repository interfaces and models (frozen). If you need data the contract does not give you, ask Brilian/Kemas to extend it (change procedure), do not query SQLite from the view layer.
**Acceptance:** AC-01, AC-02 (display side), AC-03/04/05/07/09 manual steps in `TESTING.md` pass; no unhandled exception dialog during the demo script (`DEMO_SCRIPT.md`); every control has a visible label and keyboard access.

## Brilian: data, persistence, business rules

**Already done:** schema, database initialization, once-only import from the embedded case data, four SQLite repositories, validators, `PriorityService`, 79 tests (`TESTING.md`).

**To do:**
1. Re-run `python3 scripts/extract_seed.py` and confirm `aether_seed.json` is unchanged (`git diff`); spot-check 10 incidents, the 5 assets, and 20 actions against `bundle.js` using `DATA_DICTIONARY.md`.
2. Review the priority rule (`DECISIONS.md` D-07) with the team: thresholds, 28-day window, point weights. If you change anything: update `PriorityService`, `PriorityServiceTests`, D-07, the expander text in `PriorityView.xaml` and the DEMO numbers together.
3. Review the schema for missing indexes/constraints you want; any schema change needs a `SchemaVersion` bump and migration step (`CONTRIBUTING.md`).
4. Add tests for anything you find that is not covered (ideas: very long text, a source action whose incident was deleted before import, concurrent open of the same file).
5. Decide whether the hourly readings (D-09) should be a post-MVP extension and note it in `DECISIONS.md`; do not add them to the MVP without a contract change.

**Depends on:** Core models/interfaces (frozen). Ferdian consumes your `DashboardStats` and `PriorityResult`; tell him before changing their shape.
**Acceptance:** all tests pass on Windows and macOS; import is repeatable (no duplicates after restart); AC-02 numbers match the source totals (380 incidents, 2,261.1 h, k USD 61,886.46 actual); no SQL built from user text.

---

## Integration procedure

Branches (all start from the same `main` snapshot that contains this foundation):

```
main                      protected: build + tests must pass
feature/ui-<topic>        Ferdian
feature/data-<topic>      Brilian
feature/foundation-<topic> Kemas
```

1. Small branches, one topic each. Commit only your own area; do not reformat other people's files.
2. Before opening a PR: `git fetch`, merge or rebase onto the latest `main`, then `dotnet build Aether.sln -c Release` and `dotnet test Aether.sln -c Release`.
3. PR reviewer = owner of the touched area (table above). A PR that touches two areas needs both owners.
4. Merge order when several land together: contracts (Core models/interfaces) -> Infrastructure -> Desktop. Re-run build and tests after each merge.
5. Kemas does a Windows smoke run (`dotnet run`, one pass through `DEMO_SCRIPT.md`) after merging to `main` and before any release build.

### If a teammate already has local work from before this foundation

Do not overwrite it and do not assume it merges cleanly.

1. Put your work on a branch: `git switch -c feature/<name>-previous` and commit it there.
2. Compare it to `main` by area: `git diff main --stat`. Keep whichever side satisfies the PRD acceptance criteria and the frozen contracts; port the good parts of the other side.
3. If your code used another model shape (for example different field names), adapt your code to the Core models in `main`, or propose a contract change (`CONTRIBUTING.md`). Do not fork the models.
4. If you used SQL Server or a different table layout, move to the SQLite schema in `DATABASE.md`; do not keep two databases.
5. Open a PR from a branch cut from `main` that contains only the ported changes; run build and tests before asking for review.

## Definition of done for the MVP

- Every Must item in PRD section 7 works on Windows, verified by the checklist in `TESTING.md` (record who ran it and when).
- `dotnet build` and `dotnet test` pass on Windows.
- Release folder runs on a clean Windows machine without .NET or a database server.
- README, `TESTING.md` and `PRD.md` section 17 match what the app really does.
