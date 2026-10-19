using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Aether.Core;
using Aether.Core.Models;
using Aether.Infrastructure.Data;
using Microsoft.Data.Sqlite;

namespace Aether.Infrastructure.Seed;

public record SeedReport(
    bool Applied, int Incidents, int Assets, int ConditionRecords, int Actions, IReadOnlyList<string> Warnings)
{
    public static SeedReport AlreadyApplied { get; } =
        new(false, 0, 0, 0, 0, Array.Empty<string>());
}

/// <summary>
/// Loads the case data (embedded aether_seed.json, produced by scripts/extract_seed.py) into an empty
/// database once. A row in SeedLog records that the import ran, so restarts never duplicate data and
/// rows the user deleted do not come back.
/// </summary>
public static class SourceDataImporter
{
    public const string SeedKey = "case-data-v1";
    private const string ResourceName = "Aether.Seed.aether_seed.json";

    public static SeedReport EnsureSeeded(AetherDatabase db)
    {
        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
                ?? throw new AetherException("The embedded case data file is missing from the application.");
            return EnsureSeeded(db, stream);
        }
        catch (JsonException ex)
        {
            throw new AetherException("The embedded case data file could not be read: " + ex.Message, ex);
        }
        catch (SqliteException ex)
        {
            throw new AetherException("Importing the case data failed: " + ex.Message, ex);
        }
    }

    /// <summary>Overload used by tests with their own JSON.</summary>
    public static SeedReport EnsureSeeded(AetherDatabase db, Stream json)
    {
        using var connection = db.OpenConnection();
        using var tx = connection.BeginTransaction();

        using (var check = connection.CreateCommand())
        {
            check.Transaction = tx;
            check.CommandText = "SELECT COUNT(*) FROM SeedLog WHERE SeedKey = @k;";
            check.Parameters.AddWithValue("@k", SeedKey);
            if (Convert.ToInt32(check.ExecuteScalar()) > 0) return SeedReport.AlreadyApplied;
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var warnings = new List<string>();
        int incidents = 0, assets = 0, records = 0, actions = 0;

        var serialToId = new Dictionary<int, int>();
        foreach (var e in Items(root, "incidents"))
        {
            var serial = Int(e, "sourceSerialNo");
            var title = Str(e, "title");
            var plant = Str(e, "plant");
            var tag = Str(e, "tagNumber");
            var status = Str(e, "status");
            if (serial is null || title is null || plant is null || tag is null
                || status is null || !IncidentStatuses.All.Contains(status))
            {
                warnings.Add($"Incident serial {serial?.ToString() ?? "?"} skipped: required value missing or invalid.");
                continue;
            }

            using var cmd = connection.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT OR IGNORE INTO Incidents
                  (SourceSerialNo, Reference, ArNo, Title, Plant, TagNumber, EquipmentClass, EquipmentType,
                   Discipline, Component, FailureMechanism, HighestImpact, PreRisk, RiskScore, PicRca, Status,
                   OccurredOn, RcaDueDate, DowntimeHours, ActualLossKUsd, PotentialLossKUsd, Origin)
                VALUES
                  (@serial, @reference, @arNo, @title, @plant, @tag, @eqClass, @eqType,
                   @discipline, @component, @mechanism, @impact, @preRisk, @riskScore, @pic, @status,
                   @occurred, @rcaDue, @downtime, @actual, @potential, 'Source');
                """;
            var p = cmd.Parameters;
            p.AddWithValue("@serial", serial.Value);
            p.AddWithValue("@reference", SqlHelpers.ToDb(Str(e, "mtoNo")));
            p.AddWithValue("@arNo", SqlHelpers.ToDb(Str(e, "arNo")));
            p.AddWithValue("@title", title);
            p.AddWithValue("@plant", plant);
            p.AddWithValue("@tag", tag);
            p.AddWithValue("@eqClass", SqlHelpers.ToDb(Str(e, "eqClass")));
            p.AddWithValue("@eqType", SqlHelpers.ToDb(Str(e, "eqType")));
            p.AddWithValue("@discipline", SqlHelpers.ToDb(Str(e, "discipline")));
            p.AddWithValue("@component", SqlHelpers.ToDb(Str(e, "component")));
            p.AddWithValue("@mechanism", SqlHelpers.ToDb(Str(e, "failureMechanism")));
            p.AddWithValue("@impact", SqlHelpers.ToDb(Str(e, "highestImpact")));
            p.AddWithValue("@preRisk", SqlHelpers.ToDb(Str(e, "preRisk")));
            p.AddWithValue("@riskScore", SqlHelpers.ToDb(NonNegative(Dbl(e, "riskScore"))));
            p.AddWithValue("@pic", SqlHelpers.ToDb(Str(e, "picRca")));
            p.AddWithValue("@status", status);
            p.AddWithValue("@occurred", ValidDate(e, "occurredOn", serial.Value, warnings));
            p.AddWithValue("@rcaDue", ValidDate(e, "rcaDueDate", serial.Value, warnings));
            p.AddWithValue("@downtime", SqlHelpers.ToDb(NonNegative(Dbl(e, "downtimeHours"))));
            p.AddWithValue("@actual", SqlHelpers.ToDb(NonNegative(Dbl(e, "actualLossKUsd"))));
            p.AddWithValue("@potential", SqlHelpers.ToDb(NonNegative(Dbl(e, "potentialLossKUsd"))));
            if (cmd.ExecuteNonQuery() == 1) incidents++;
        }

        // Map serial numbers to the IDs that were just assigned.
        using (var map = connection.CreateCommand())
        {
            map.Transaction = tx;
            map.CommandText = "SELECT SourceSerialNo, Id FROM Incidents WHERE SourceSerialNo IS NOT NULL;";
            using var r = map.ExecuteReader();
            while (r.Read()) serialToId[r.GetInt32(0)] = r.GetInt32(1);
        }

        var assetIds = new Dictionary<string, int>();
        foreach (var e in Items(root, "assets"))
        {
            var tag = Str(e, "tag");
            var name = Str(e, "name");
            if (tag is null || name is null)
            {
                warnings.Add("An asset was skipped: tag or name missing.");
                continue;
            }
            using var cmd = connection.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT OR IGNORE INTO Assets
                  (Tag, Name, Type, EquipmentClass, PlantUnit, Discipline, Criticality, DesignLife,
                   MonitoringMethod, LinkedArNo, DominantFailureMode)
                VALUES (@tag, @name, @type, @class, @plant, @discipline, @criticality, @life, @method, @ar, @mode);
                SELECT Id FROM Assets WHERE Tag = @tag;
                """;
            var p = cmd.Parameters;
            p.AddWithValue("@tag", tag);
            p.AddWithValue("@name", name);
            p.AddWithValue("@type", SqlHelpers.ToDb(Str(e, "type")));
            p.AddWithValue("@class", SqlHelpers.ToDb(Str(e, "eqClass")));
            p.AddWithValue("@plant", SqlHelpers.ToDb(Str(e, "plantUnit")));
            p.AddWithValue("@discipline", SqlHelpers.ToDb(Str(e, "discipline")));
            p.AddWithValue("@criticality", SqlHelpers.ToDb(Str(e, "criticality")));
            p.AddWithValue("@life", SqlHelpers.ToDb(Str(e, "designLife")));
            p.AddWithValue("@method", SqlHelpers.ToDb(Str(e, "monitoringMethod")));
            p.AddWithValue("@ar", SqlHelpers.ToDb(Str(e, "linkedArNo")));
            p.AddWithValue("@mode", SqlHelpers.ToDb(Str(e, "dominantFailureMode")));
            assetIds[tag] = Convert.ToInt32(cmd.ExecuteScalar());
            assets++;
        }

        foreach (var e in Items(root, "conditionRecords"))
        {
            var tag = Str(e, "assetTag");
            var parameter = Str(e, "parameter");
            var date = SqlHelpers.TryParseDate(Str(e, "measuredOn"));
            var value = Dbl(e, "value");
            var direction = Str(e, "limitDirection");
            if (tag is null || !assetIds.TryGetValue(tag, out var assetId) || parameter is null
                || date is null || value is null || (direction is not null && direction is not ("high" or "low")))
            {
                warnings.Add($"A condition record for {tag ?? "?"} was skipped: asset, parameter, date or value invalid.");
                continue;
            }
            using var cmd = connection.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT OR IGNORE INTO ConditionRecords
                  (AssetId, WeekNo, MeasuredOn, Parameter, Unit, Value, AlarmLimit, TripLimit,
                   LimitDirection, HealthStatus, Remark)
                VALUES (@asset, @week, @date, @parameter, @unit, @value, @alarm, @trip, @direction, @health, @remark);
                """;
            var p = cmd.Parameters;
            p.AddWithValue("@asset", assetId);
            p.AddWithValue("@week", Int(e, "weekNo") ?? 0);
            p.AddWithValue("@date", date.Value.ToString(SqlHelpers.DateFormat, CultureInfo.InvariantCulture));
            p.AddWithValue("@parameter", parameter);
            p.AddWithValue("@unit", SqlHelpers.ToDb(Str(e, "unit")));
            p.AddWithValue("@value", value.Value);
            p.AddWithValue("@alarm", SqlHelpers.ToDb(Dbl(e, "alarmLimit")));
            p.AddWithValue("@trip", SqlHelpers.ToDb(Dbl(e, "tripLimit")));
            p.AddWithValue("@direction", SqlHelpers.ToDb(direction));
            p.AddWithValue("@health", SqlHelpers.ToDb(Str(e, "healthStatus")));
            p.AddWithValue("@remark", SqlHelpers.ToDb(Str(e, "remark")));
            if (cmd.ExecuteNonQuery() == 1) records++;
        }

        foreach (var e in Items(root, "actions"))
        {
            var serial = Int(e, "incidentSerialNo");
            var description = Str(e, "description");
            var owner = Str(e, "owner");
            var target = SqlHelpers.TryParseDate(Str(e, "targetDate"));
            var status = Str(e, "status");
            if (serial is null || !serialToId.TryGetValue(serial.Value, out var incidentId)
                || description is null || owner is null || target is null || !ActionStatus.IsValid(status))
            {
                warnings.Add($"An action for incident serial {serial?.ToString() ?? "?"} was skipped: incident or required value missing.");
                continue;
            }
            using var cmd = connection.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO ActionItems (IncidentId, Description, Owner, TargetDate, Status, Category, RootCauseCode, Origin)
                VALUES (@incident, @description, @owner, @target, @status, @category, @rc, 'Source');
                """;
            var p = cmd.Parameters;
            p.AddWithValue("@incident", incidentId);
            p.AddWithValue("@description", description);
            p.AddWithValue("@owner", owner);
            p.AddWithValue("@target", target.Value.ToString(SqlHelpers.DateFormat, CultureInfo.InvariantCulture));
            p.AddWithValue("@status", status!);
            p.AddWithValue("@category", SqlHelpers.ToDb(Str(e, "category")));
            p.AddWithValue("@rc", SqlHelpers.ToDb(Str(e, "rootCauseCode")));
            cmd.ExecuteNonQuery();
            actions++;
        }

        using (var log = connection.CreateCommand())
        {
            log.Transaction = tx;
            log.CommandText = "INSERT INTO SeedLog (SeedKey, AppliedAt) VALUES (@k, @t);";
            log.Parameters.AddWithValue("@k", SeedKey);
            log.Parameters.AddWithValue("@t", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
            log.ExecuteNonQuery();
        }
        tx.Commit();
        return new SeedReport(true, incidents, assets, records, actions, warnings);
    }

    private static IEnumerable<JsonElement> Items(JsonElement root, string name) =>
        root.TryGetProperty(name, out var arr) && arr.ValueKind == JsonValueKind.Array
            ? arr.EnumerateArray()
            : Enumerable.Empty<JsonElement>();

    private static string? Str(JsonElement e, string name)
    {
        if (!e.TryGetProperty(name, out var v) || v.ValueKind != JsonValueKind.String) return null;
        var s = v.GetString();
        return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }

    private static double? Dbl(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number
        && v.TryGetDouble(out var d) && double.IsFinite(d) ? d : null;

    private static int? Int(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i) ? i : null;

    /// <summary>Negative numbers are not valid for these columns; they are stored as "not recorded".</summary>
    private static double? NonNegative(double? v) => v is >= 0 ? v : null;

    private static object ValidDate(JsonElement e, string name, int serial, List<string> warnings)
    {
        var text = Str(e, name);
        if (text is null) return DBNull.Value;
        var d = SqlHelpers.TryParseDate(text);
        if (d is null)
        {
            warnings.Add($"Incident serial {serial}: {name} '{text}' is not a valid date and was stored as empty.");
            return DBNull.Value;
        }
        return d.Value.ToString(SqlHelpers.DateFormat, CultureInfo.InvariantCulture);
    }
}
