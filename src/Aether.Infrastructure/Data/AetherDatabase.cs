using Aether.Core;
using Microsoft.Data.Sqlite;

namespace Aether.Infrastructure.Data;

/// <summary>
/// Owns the SQLite file: location, schema creation and connection creation.
/// Every repository opens a short-lived connection through <see cref="OpenConnection"/>.
/// </summary>
public sealed class AetherDatabase
{
    public const int SchemaVersion = 1;

    public string DatabasePath { get; }

    public AetherDatabase(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
            throw new ArgumentException("Database path is required.", nameof(databasePath));
        DatabasePath = databasePath;
    }

    /// <summary>%LOCALAPPDATA%\AetherDesktop\aether.db on Windows. Not inside the install folder.</summary>
    public static string DefaultPath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AetherDesktop",
            "aether.db");

    /// <summary>Creates the folder, the file and the tables if they do not exist. Safe to call on every start.</summary>
    public void Initialize()
    {
        try
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(DatabasePath));
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            using var connection = OpenConnection();
            var version = Convert.ToInt32(ExecuteScalar(connection, "PRAGMA user_version;"));
            if (version > SchemaVersion)
                throw new AetherException(
                    $"The database at {DatabasePath} was created by a newer version of the application (schema {version}).");

            using var tx = connection.BeginTransaction();
            using (var cmd = connection.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = Schema;
                cmd.ExecuteNonQuery();
            }
            using (var cmd = connection.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = $"PRAGMA user_version = {SchemaVersion};";
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }
        catch (SqliteException ex)
        {
            throw new AetherException($"The database could not be opened or created at {DatabasePath}: {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            throw new AetherException($"The database folder could not be prepared: {ex.Message}", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new AetherException($"No permission to use the database folder: {ex.Message}", ex);
        }
    }

    /// <summary>Opens a connection with foreign keys enforced. The caller disposes it.</summary>
    public SqliteConnection OpenConnection()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            ForeignKeys = true,
            Pooling = false,
        };
        var connection = new SqliteConnection(builder.ToString());
        connection.Open();
        return connection;
    }

    private static object? ExecuteScalar(SqliteConnection connection, string sql)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        return cmd.ExecuteScalar();
    }

    private const string Schema = """
        CREATE TABLE IF NOT EXISTS Incidents (
            Id                INTEGER PRIMARY KEY AUTOINCREMENT,
            SourceSerialNo    INTEGER UNIQUE,
            Reference         TEXT,
            ArNo              TEXT,
            Title             TEXT NOT NULL CHECK (length(trim(Title)) > 0),
            Plant             TEXT NOT NULL CHECK (length(trim(Plant)) > 0),
            TagNumber         TEXT NOT NULL CHECK (length(trim(TagNumber)) > 0),
            EquipmentClass    TEXT,
            EquipmentType     TEXT,
            Discipline        TEXT,
            Component         TEXT,
            FailureMechanism  TEXT,
            HighestImpact     TEXT,
            PreRisk           TEXT,
            RiskScore         REAL CHECK (RiskScore IS NULL OR RiskScore >= 0),
            PicRca            TEXT,
            Status            TEXT NOT NULL CHECK (Status IN
                ('NEW REGISTERED','RCA PROCESS','CA/PA EXECUTION','MONITORING RESULT','RISK CLOSED','RISK CANCELED')),
            OccurredOn        TEXT,
            RcaDueDate        TEXT,
            DowntimeHours     REAL CHECK (DowntimeHours IS NULL OR DowntimeHours >= 0),
            ActualLossKUsd    REAL CHECK (ActualLossKUsd IS NULL OR ActualLossKUsd >= 0),
            PotentialLossKUsd REAL CHECK (PotentialLossKUsd IS NULL OR PotentialLossKUsd >= 0),
            Origin            TEXT NOT NULL DEFAULT 'User' CHECK (Origin IN ('Source','User'))
        );
        CREATE INDEX IF NOT EXISTS IX_Incidents_TagNumber  ON Incidents(TagNumber);
        CREATE INDEX IF NOT EXISTS IX_Incidents_Status     ON Incidents(Status);
        CREATE INDEX IF NOT EXISTS IX_Incidents_Plant      ON Incidents(Plant);
        CREATE INDEX IF NOT EXISTS IX_Incidents_OccurredOn ON Incidents(OccurredOn);

        CREATE TABLE IF NOT EXISTS Assets (
            Id                  INTEGER PRIMARY KEY AUTOINCREMENT,
            Tag                 TEXT NOT NULL UNIQUE,
            Name                TEXT NOT NULL,
            Type                TEXT,
            EquipmentClass      TEXT,
            PlantUnit           TEXT,
            Discipline          TEXT,
            Criticality         TEXT,
            DesignLife          TEXT,
            MonitoringMethod    TEXT,
            LinkedArNo          TEXT,
            DominantFailureMode TEXT
        );

        CREATE TABLE IF NOT EXISTS ConditionRecords (
            Id             INTEGER PRIMARY KEY AUTOINCREMENT,
            AssetId        INTEGER NOT NULL REFERENCES Assets(Id) ON DELETE CASCADE,
            WeekNo         INTEGER NOT NULL,
            MeasuredOn     TEXT NOT NULL,
            Parameter      TEXT NOT NULL,
            Unit           TEXT,
            Value          REAL NOT NULL,
            AlarmLimit     REAL,
            TripLimit      REAL,
            LimitDirection TEXT CHECK (LimitDirection IS NULL OR LimitDirection IN ('high','low')),
            HealthStatus   TEXT,
            Remark         TEXT,
            UNIQUE (AssetId, MeasuredOn, Parameter)
        );
        CREATE INDEX IF NOT EXISTS IX_ConditionRecords_Asset ON ConditionRecords(AssetId, MeasuredOn);

        CREATE TABLE IF NOT EXISTS ActionItems (
            Id            INTEGER PRIMARY KEY AUTOINCREMENT,
            IncidentId    INTEGER NOT NULL REFERENCES Incidents(Id) ON DELETE CASCADE,
            Description   TEXT NOT NULL CHECK (length(trim(Description)) > 0),
            Owner         TEXT NOT NULL CHECK (length(trim(Owner)) > 0),
            TargetDate    TEXT NOT NULL,
            Status        TEXT NOT NULL CHECK (Status IN ('Open','In Progress','Closed')),
            Category      TEXT,
            RootCauseCode TEXT,
            Origin        TEXT NOT NULL DEFAULT 'User' CHECK (Origin IN ('Source','User'))
        );
        CREATE INDEX IF NOT EXISTS IX_ActionItems_Incident ON ActionItems(IncidentId);
        CREATE INDEX IF NOT EXISTS IX_ActionItems_Status   ON ActionItems(Status);

        CREATE TABLE IF NOT EXISTS SeedLog (
            SeedKey   TEXT PRIMARY KEY,
            AppliedAt TEXT NOT NULL
        );
        """;
}
