# Database

SQLite through `Microsoft.Data.Sqlite`, using the ADO.NET pattern from the PBKK labs (`using` connection, command with
parameters, reader) in repository classes. No SQL Server is needed.

## Location

| Item | Value |
|---|---|
| Default file | `%LOCALAPPDATA%\AetherDesktop\aether.db` (for example `C:\Users\<name>\AppData\Local\AetherDesktop\aether.db`) |
| Override | environment variable `AETHER_DB_PATH` = full path of the file to use |
| Why there | The install/publish folder may be read-only; each Windows user gets their own data |

The window's sidebar shows the path in use.

## Initialization and import

On every start `AppServices.Create()` does this:

1. Creates the folder and file if missing, then runs the schema script (`CREATE TABLE IF NOT EXISTS`, safe to repeat)
   inside one transaction and sets `PRAGMA user_version = 1`. A database with a higher version is refused with a message.
2. `SourceDataImporter.EnsureSeeded` imports the case data **once**. It checks the `SeedLog` table for the key
   `case-data-v1`. If present, nothing happens. If absent, all rows are inserted in one transaction together with
   the log row. A failure rolls everything back.
3. Result: 380 incidents, 5 assets, 520 condition records, 20 actions on first start; identical data afterwards. Restarting
   never duplicates rows, and incidents you delete do not come back. Source rows are also protected by the unique
   `SourceSerialNo`.

Malformed source values are handled safely: rows with missing required text or an invalid status are skipped and
reported as warnings, negative numbers and unreadable dates are stored as NULL. The embedded file currently produces
no warnings (tested).

## Schema

| Table | Key | Relations and constraints |
|---|---|---|
| `Incidents` | `Id` AUTOINCREMENT; `SourceSerialNo` UNIQUE | `Status` CHECK (6 values); `Title`, `Plant`, `TagNumber` not blank; downtime, losses, risk score >= 0 or NULL; `Origin` CHECK. Indexes on tag, status, plant, date. |
| `Assets` | `Id`; `Tag` UNIQUE | none |
| `ConditionRecords` | `Id`; UNIQUE (`AssetId`, `MeasuredOn`, `Parameter`) | `AssetId` FK to `Assets` (cascade); `LimitDirection` CHECK high/low |
| `ActionItems` | `Id` | `IncidentId` FK to `Incidents` **ON DELETE CASCADE**; `Status` CHECK (Open, In Progress, Closed); `Description`, `Owner` not blank |
| `SeedLog` | `SeedKey` | records finished imports |

Every connection sets `Foreign Keys=True`. Column meaning and source fields: `DATA_DICTIONARY.md`.

## Referential integrity

An action must reference an existing incident (foreign key and a repository check). Deleting an incident that still has
actions deletes those actions in the same transaction. The UI first shows how many actions will be removed and asks for
confirmation (decision D-05).

## Safe reset

1. Close the application.
2. Run `scripts\reset-database.ps1`. It **moves** `aether.db` (and `-wal`/`-shm`) to `aether.db.bak-<timestamp>` instead of
   deleting it.
3. Start the application: a new database is created and the case data is imported again.

To restore your edits, close the app and move the backup file back to `aether.db`. To start a demo from clean
data at any time, use the same procedure.

## Tests

Persistence tests use a temporary file per test (`tests/Aether.Infrastructure.Tests/TestDb.cs`) and never touch the user's
database. They cover initialization, repeatable import, malformed import data, CRUD, search/filter, foreign keys,
cascade delete, dashboard aggregates and restart persistence.
