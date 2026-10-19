# Contributing

## Setup

1. Windows 10/11 x64, Git, and the .NET SDK 8 or newer (`dotnet --list-sdks`). Visual Studio 2022 with the ".NET desktop development" workload is the easiest, VS Code also works.
2. `git clone` the repository, then `dotnet restore Aether.sln`. Packages come from nuget.org (Microsoft.Data.Sqlite and the xUnit test packages).
3. `dotnet build Aether.sln` and `dotnet test Aether.sln` should pass before you change anything.
4. Run the app: `dotnet run --project src/Aether.Desktop` (or F5 in Visual Studio with `Aether.Desktop` as startup project).

On macOS/Linux only the build and the tests work; see the README.

## Branches and ownership

- Work on a branch named `feature/<topic>` (for example `feature/priority-tweaks`), open a pull request into `main`, get one teammate to review.
- Primary owners per area are in `TEAM_TASKS.md`. Anyone may edit any file, but tell the owner and keep their conventions. Avoid two people editing the same file at once: split by owned folder.
- Never commit `bin/`, `obj/`, `*.db`, `artifacts/`, IDE folders (the `.gitignore` covers them) or the large reference files unless the team agrees.

## Code rules

- Layering: no database or WPF types in `Aether.Core`; no WPF in `Aether.Infrastructure`; no SQL in `Aether.Desktop`.
- SQL always uses parameters (`@name`); never build SQL from user text. Dispose connections, commands and readers (`using`).
- Missing value = `null`, never `0`. Show "Not recorded" or a dash.
- A rule that decides what the user sees (validation, priority, statistics) goes into Core or Infrastructure with a test.
- User-facing text: plain words, say what happened and what to do next. Do not claim AI, root-cause identification, savings or ROI.
- Style: match the surrounding file (4 spaces, file-scoped namespaces, `var` where the type is obvious, nullable reference types on).

## Changing a shared contract

Contracts are listed in `ARCHITECTURE.md` ("Contracts that must stay compatible"). When one must change:

1. Announce it in the PR title (`CONTRACT:`) and describe the change.
2. Update the interface/model, every implementation and caller, the tests, the schema (and `DATABASE.md`), `DATA_DICTIONARY.md` and `ARCHITECTURE.md` in the same PR.
3. Schema change: increase `AetherDatabase.SchemaVersion` and add a migration step; existing local databases must keep working (or document a reset in the PR).
4. `dotnet build` and `dotnet test` pass; the owner of every affected module approves.

## Tests

Add or update tests with each behaviour change (`docs/TESTING.md`). Persistence tests must use `TestDb` (temporary file). Do not weaken or delete a test to make a build pass; fix the cause.

## Before opening a pull request

```
dotnet build Aether.sln -c Release
dotnet test  Aether.sln -c Release
```
For UI changes, run the app on Windows and walk the affected screen, including an empty database and a validation error. Update the docs that describe the behaviour you changed.

## Updating the source data

The reference data is read-only. If the case data changes: edit nothing in `references/`; replace the file, run `python3 scripts/extract_seed.py`, review the diff of
`aether_seed.json`, bump `SourceDataImporter.SeedKey` if existing databases should re-import, and update `DATA_DICTIONARY.md` and the import tests.
