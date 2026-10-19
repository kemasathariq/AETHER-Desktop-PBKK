using Aether.Core;
using Aether.Core.Models;
using Aether.Core.Repositories;
using Aether.Core.Validation;
using Aether.Infrastructure.Data;
using Microsoft.Data.Sqlite;

namespace Aether.Infrastructure.Repositories;

public sealed class SqliteIncidentRepository : IIncidentRepository
{
    private const string SelectColumns = """
        Id, SourceSerialNo, Reference, ArNo, Title, Plant, TagNumber, EquipmentClass, EquipmentType,
        Discipline, Component, FailureMechanism, HighestImpact, PreRisk, RiskScore, PicRca, Status,
        OccurredOn, RcaDueDate, DowntimeHours, ActualLossKUsd, PotentialLossKUsd, Origin
        """;

    private readonly AetherDatabase _db;

    public SqliteIncidentRepository(AetherDatabase db) => _db = db;

    public IReadOnlyList<Incident> Search(IncidentFilter? filter = null) =>
        SqlHelpers.Run("Loading incidents", () =>
        {
            var where = new List<string>();
            using var connection = _db.OpenConnection();
            using var cmd = connection.CreateCommand();

            if (!string.IsNullOrWhiteSpace(filter?.Text))
            {
                where.Add("""
                    (Title LIKE @text ESCAPE '\' OR TagNumber LIKE @text ESCAPE '\' OR Reference LIKE @text ESCAPE '\'
                     OR ArNo LIKE @text ESCAPE '\' OR Plant LIKE @text ESCAPE '\' OR Component LIKE @text ESCAPE '\'
                     OR FailureMechanism LIKE @text ESCAPE '\')
                    """);
                cmd.Parameters.AddWithValue("@text", "%" + SqlHelpers.EscapeLike(filter!.Text!.Trim()) + "%");
            }
            AddEquals(where, cmd, "Status", "@status", filter?.Status);
            AddEquals(where, cmd, "Plant", "@plant", filter?.Plant);
            AddEquals(where, cmd, "Discipline", "@discipline", filter?.Discipline);
            if (filter?.ActiveOnly == true)
                where.Add("Status NOT IN ('RISK CLOSED','RISK CANCELED')");

            cmd.CommandText = $"SELECT {SelectColumns} FROM Incidents"
                + (where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "")
                + " ORDER BY OccurredOn DESC, Id DESC;";
            return ReadAll(cmd);
        });

    public Incident? GetById(int id) =>
        SqlHelpers.Run("Loading the incident", () =>
        {
            using var connection = _db.OpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT {SelectColumns} FROM Incidents WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            return ReadAll(cmd).FirstOrDefault();
        });

    public int Add(Incident incident)
    {
        IncidentValidator.Validate(incident).ThrowIfInvalid();
        return SqlHelpers.Run("Saving the incident", () =>
        {
            using var connection = _db.OpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                INSERT INTO Incidents
                  (SourceSerialNo, Reference, ArNo, Title, Plant, TagNumber, EquipmentClass, EquipmentType,
                   Discipline, Component, FailureMechanism, HighestImpact, PreRisk, RiskScore, PicRca, Status,
                   OccurredOn, RcaDueDate, DowntimeHours, ActualLossKUsd, PotentialLossKUsd, Origin)
                VALUES
                  (@serial, @reference, @arNo, @title, @plant, @tag, @eqClass, @eqType,
                   @discipline, @component, @mechanism, @impact, @preRisk, @riskScore, @pic, @status,
                   @occurred, @rcaDue, @downtime, @actual, @potential, @origin);
                SELECT last_insert_rowid();
                """;
            Bind(cmd, incident);
            return Convert.ToInt32(cmd.ExecuteScalar());
        });
    }

    public void Update(Incident incident)
    {
        IncidentValidator.Validate(incident).ThrowIfInvalid();
        SqlHelpers.Run("Saving the incident", () =>
        {
            using var connection = _db.OpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                UPDATE Incidents SET
                  Reference = @reference, ArNo = @arNo, Title = @title, Plant = @plant, TagNumber = @tag,
                  EquipmentClass = @eqClass, EquipmentType = @eqType, Discipline = @discipline,
                  Component = @component, FailureMechanism = @mechanism, HighestImpact = @impact,
                  PreRisk = @preRisk, RiskScore = @riskScore, PicRca = @pic, Status = @status,
                  OccurredOn = @occurred, RcaDueDate = @rcaDue, DowntimeHours = @downtime,
                  ActualLossKUsd = @actual, PotentialLossKUsd = @potential
                WHERE Id = @id;
                """;
            Bind(cmd, incident);
            cmd.Parameters.AddWithValue("@id", incident.Id);
            if (cmd.ExecuteNonQuery() == 0) throw new NotFoundException("Incident", incident.Id);
        });
    }

    public int CountActions(int incidentId) =>
        SqlHelpers.Run("Counting related actions", () =>
        {
            using var connection = _db.OpenConnection();
            return CountActions(connection, null, incidentId);
        });

    public int Delete(int incidentId) =>
        SqlHelpers.Run("Deleting the incident", () =>
        {
            using var connection = _db.OpenConnection();
            using var tx = connection.BeginTransaction();
            var actions = CountActions(connection, tx, incidentId);

            using (var cmd = connection.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM ActionItems WHERE IncidentId = @id;";
                cmd.Parameters.AddWithValue("@id", incidentId);
                cmd.ExecuteNonQuery();
            }
            using (var cmd = connection.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM Incidents WHERE Id = @id;";
                cmd.Parameters.AddWithValue("@id", incidentId);
                if (cmd.ExecuteNonQuery() == 0) throw new NotFoundException("Incident", incidentId);
            }
            tx.Commit();
            return actions;
        });

    public IReadOnlyList<string> GetPlants() => Distinct("Plant");

    public IReadOnlyList<string> GetDisciplines() => Distinct("Discipline");

    private IReadOnlyList<string> Distinct(string column) =>
        SqlHelpers.Run("Loading filter values", () =>
        {
            using var connection = _db.OpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT DISTINCT {column} FROM Incidents WHERE {column} IS NOT NULL ORDER BY {column};";
            var list = new List<string>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) list.Add(reader.GetString(0));
            return list;
        });

    private static int CountActions(SqliteConnection connection, SqliteTransaction? tx, int incidentId)
    {
        using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "SELECT COUNT(*) FROM ActionItems WHERE IncidentId = @id;";
        cmd.Parameters.AddWithValue("@id", incidentId);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static void AddEquals(List<string> where, SqliteCommand cmd, string column, string param, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        where.Add($"{column} = {param}");
        cmd.Parameters.AddWithValue(param, value);
    }

    private static void Bind(SqliteCommand cmd, Incident i)
    {
        var p = cmd.Parameters;
        p.AddWithValue("@serial", SqlHelpers.ToDb(i.SourceSerialNo));
        p.AddWithValue("@reference", SqlHelpers.ToDb(i.Reference));
        p.AddWithValue("@arNo", SqlHelpers.ToDb(i.ArNo));
        p.AddWithValue("@title", i.Title.Trim());
        p.AddWithValue("@plant", i.Plant.Trim());
        p.AddWithValue("@tag", i.TagNumber.Trim());
        p.AddWithValue("@eqClass", SqlHelpers.ToDb(i.EquipmentClass));
        p.AddWithValue("@eqType", SqlHelpers.ToDb(i.EquipmentType));
        p.AddWithValue("@discipline", SqlHelpers.ToDb(i.Discipline));
        p.AddWithValue("@component", SqlHelpers.ToDb(i.Component));
        p.AddWithValue("@mechanism", SqlHelpers.ToDb(i.FailureMechanism));
        p.AddWithValue("@impact", SqlHelpers.ToDb(i.HighestImpact));
        p.AddWithValue("@preRisk", SqlHelpers.ToDb(i.PreRisk));
        p.AddWithValue("@riskScore", SqlHelpers.ToDb(i.RiskScore));
        p.AddWithValue("@pic", SqlHelpers.ToDb(i.PicRca));
        p.AddWithValue("@status", i.Status);
        p.AddWithValue("@occurred", SqlHelpers.ToDb(i.OccurredOn));
        p.AddWithValue("@rcaDue", SqlHelpers.ToDb(i.RcaDueDate));
        p.AddWithValue("@downtime", SqlHelpers.ToDb(i.DowntimeHours));
        p.AddWithValue("@actual", SqlHelpers.ToDb(i.ActualLossKUsd));
        p.AddWithValue("@potential", SqlHelpers.ToDb(i.PotentialLossKUsd));
        p.AddWithValue("@origin", i.Origin);
    }

    internal static List<Incident> ReadAll(SqliteCommand cmd)
    {
        var list = new List<Incident>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new Incident
            {
                Id = r.GetInt32(r.GetOrdinal("Id")),
                SourceSerialNo = SqlHelpers.GetInt(r, "SourceSerialNo"),
                Reference = SqlHelpers.GetString(r, "Reference"),
                ArNo = SqlHelpers.GetString(r, "ArNo"),
                Title = SqlHelpers.GetRequiredString(r, "Title"),
                Plant = SqlHelpers.GetRequiredString(r, "Plant"),
                TagNumber = SqlHelpers.GetRequiredString(r, "TagNumber"),
                EquipmentClass = SqlHelpers.GetString(r, "EquipmentClass"),
                EquipmentType = SqlHelpers.GetString(r, "EquipmentType"),
                Discipline = SqlHelpers.GetString(r, "Discipline"),
                Component = SqlHelpers.GetString(r, "Component"),
                FailureMechanism = SqlHelpers.GetString(r, "FailureMechanism"),
                HighestImpact = SqlHelpers.GetString(r, "HighestImpact"),
                PreRisk = SqlHelpers.GetString(r, "PreRisk"),
                RiskScore = SqlHelpers.GetDouble(r, "RiskScore"),
                PicRca = SqlHelpers.GetString(r, "PicRca"),
                Status = SqlHelpers.GetRequiredString(r, "Status"),
                OccurredOn = SqlHelpers.GetDate(r, "OccurredOn"),
                RcaDueDate = SqlHelpers.GetDate(r, "RcaDueDate"),
                DowntimeHours = SqlHelpers.GetDouble(r, "DowntimeHours"),
                ActualLossKUsd = SqlHelpers.GetDouble(r, "ActualLossKUsd"),
                PotentialLossKUsd = SqlHelpers.GetDouble(r, "PotentialLossKUsd"),
                Origin = SqlHelpers.GetRequiredString(r, "Origin"),
            });
        }
        return list;
    }
}
