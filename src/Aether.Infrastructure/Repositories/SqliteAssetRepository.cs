using Aether.Core.Models;
using Aether.Core.Repositories;
using Aether.Infrastructure.Data;
using Microsoft.Data.Sqlite;

namespace Aether.Infrastructure.Repositories;

/// <summary>Read-only access to assets and their condition records (both come from the source import).</summary>
public sealed class SqliteAssetRepository : IAssetRepository
{
    private readonly AetherDatabase _db;

    public SqliteAssetRepository(AetherDatabase db) => _db = db;

    public IReadOnlyList<Asset> GetAssets() =>
        SqlHelpers.Run("Loading assets", () =>
        {
            using var connection = _db.OpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                SELECT a.Id, a.Tag, a.Name, a.Type, a.EquipmentClass, a.PlantUnit, a.Discipline, a.Criticality,
                       a.DesignLife, a.MonitoringMethod, a.LinkedArNo, a.DominantFailureMode,
                       (SELECT COUNT(*) FROM ConditionRecords c WHERE c.AssetId = a.Id) AS RecordCount
                FROM Assets a
                ORDER BY a.Tag;
                """;
            var list = new List<Asset>();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new Asset
                {
                    Id = r.GetInt32(r.GetOrdinal("Id")),
                    Tag = SqlHelpers.GetRequiredString(r, "Tag"),
                    Name = SqlHelpers.GetRequiredString(r, "Name"),
                    Type = SqlHelpers.GetString(r, "Type"),
                    EquipmentClass = SqlHelpers.GetString(r, "EquipmentClass"),
                    PlantUnit = SqlHelpers.GetString(r, "PlantUnit"),
                    Discipline = SqlHelpers.GetString(r, "Discipline"),
                    Criticality = SqlHelpers.GetString(r, "Criticality"),
                    DesignLife = SqlHelpers.GetString(r, "DesignLife"),
                    MonitoringMethod = SqlHelpers.GetString(r, "MonitoringMethod"),
                    LinkedArNo = SqlHelpers.GetString(r, "LinkedArNo"),
                    DominantFailureMode = SqlHelpers.GetString(r, "DominantFailureMode"),
                    RecordCount = r.GetInt32(r.GetOrdinal("RecordCount")),
                });
            }
            return list;
        });

    public IReadOnlyList<ConditionRecord> GetRecords(int assetId) =>
        SqlHelpers.Run("Loading condition records", () =>
        {
            using var connection = _db.OpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = RecordSelect + " WHERE AssetId = @id ORDER BY MeasuredOn, Parameter;";
            cmd.Parameters.AddWithValue("@id", assetId);
            return ReadRecords(cmd);
        });

    public IReadOnlyList<ConditionRecord> GetRecordsByTag(string tag) =>
        SqlHelpers.Run("Loading condition records", () =>
        {
            using var connection = _db.OpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = RecordSelect
                + " WHERE AssetId = (SELECT Id FROM Assets WHERE Tag = @tag) ORDER BY MeasuredOn, Parameter;";
            cmd.Parameters.AddWithValue("@tag", tag ?? string.Empty);
            return ReadRecords(cmd);
        });

    private const string RecordSelect = """
        SELECT Id, AssetId, WeekNo, MeasuredOn, Parameter, Unit, Value, AlarmLimit, TripLimit,
               LimitDirection, HealthStatus, Remark
        FROM ConditionRecords
        """;

    private static List<ConditionRecord> ReadRecords(SqliteCommand cmd)
    {
        var list = new List<ConditionRecord>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            // A row with an unreadable date cannot be placed on a timeline, so it is skipped.
            var date = SqlHelpers.GetDate(r, "MeasuredOn");
            if (date is null) continue;
            list.Add(new ConditionRecord
            {
                Id = r.GetInt32(r.GetOrdinal("Id")),
                AssetId = r.GetInt32(r.GetOrdinal("AssetId")),
                WeekNo = r.GetInt32(r.GetOrdinal("WeekNo")),
                MeasuredOn = date.Value,
                Parameter = SqlHelpers.GetRequiredString(r, "Parameter"),
                Unit = SqlHelpers.GetString(r, "Unit"),
                Value = r.GetDouble(r.GetOrdinal("Value")),
                AlarmLimit = SqlHelpers.GetDouble(r, "AlarmLimit"),
                TripLimit = SqlHelpers.GetDouble(r, "TripLimit"),
                LimitDirection = SqlHelpers.GetString(r, "LimitDirection"),
                HealthStatus = SqlHelpers.GetString(r, "HealthStatus"),
                Remark = SqlHelpers.GetString(r, "Remark"),
            });
        }
        return list;
    }
}
