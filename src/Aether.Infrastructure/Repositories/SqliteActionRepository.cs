using Aether.Core;
using Aether.Core.Models;
using Aether.Core.Repositories;
using Aether.Core.Validation;
using Aether.Infrastructure.Data;
using Microsoft.Data.Sqlite;

namespace Aether.Infrastructure.Repositories;

public sealed class SqliteActionRepository : IActionRepository
{
    private const string SelectSql = """
        SELECT a.Id, a.IncidentId, a.Description, a.Owner, a.TargetDate, a.Status, a.Category,
               a.RootCauseCode, a.Origin, i.Title AS IncidentTitle, i.TagNumber AS IncidentTag
        FROM ActionItems a
        INNER JOIN Incidents i ON i.Id = a.IncidentId
        """;

    private readonly AetherDatabase _db;

    public SqliteActionRepository(AetherDatabase db) => _db = db;

    public IReadOnlyList<ActionItem> Search(ActionFilter? filter = null) =>
        SqlHelpers.Run("Loading actions", () =>
        {
            var where = new List<string>();
            using var connection = _db.OpenConnection();
            using var cmd = connection.CreateCommand();

            if (!string.IsNullOrWhiteSpace(filter?.Text))
            {
                where.Add("""
                    (a.Description LIKE @text ESCAPE '\' OR a.Owner LIKE @text ESCAPE '\'
                     OR i.Title LIKE @text ESCAPE '\' OR i.TagNumber LIKE @text ESCAPE '\')
                    """);
                cmd.Parameters.AddWithValue("@text", "%" + SqlHelpers.EscapeLike(filter!.Text!.Trim()) + "%");
            }
            if (!string.IsNullOrWhiteSpace(filter?.Status))
            {
                where.Add("a.Status = @status");
                cmd.Parameters.AddWithValue("@status", filter!.Status);
            }
            if (filter?.IncidentId is { } incidentId)
            {
                where.Add("a.IncidentId = @incident");
                cmd.Parameters.AddWithValue("@incident", incidentId);
            }

            cmd.CommandText = SelectSql
                + (where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "")
                + " ORDER BY a.TargetDate, a.Id;";
            return ReadAll(cmd);
        });

    public ActionItem? GetById(int id) =>
        SqlHelpers.Run("Loading the action", () =>
        {
            using var connection = _db.OpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = SelectSql + " WHERE a.Id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            return ReadAll(cmd).FirstOrDefault();
        });

    public int Add(ActionItem action)
    {
        ActionValidator.Validate(action).ThrowIfInvalid();
        return SqlHelpers.Run("Saving the action", () =>
        {
            using var connection = _db.OpenConnection();
            EnsureIncidentExists(connection, action.IncidentId);
            using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                INSERT INTO ActionItems (IncidentId, Description, Owner, TargetDate, Status, Category, RootCauseCode, Origin)
                VALUES (@incident, @description, @owner, @target, @status, @category, @rc, @origin);
                SELECT last_insert_rowid();
                """;
            Bind(cmd, action);
            return Convert.ToInt32(cmd.ExecuteScalar());
        });
    }

    public void Update(ActionItem action)
    {
        ActionValidator.Validate(action).ThrowIfInvalid();
        SqlHelpers.Run("Saving the action", () =>
        {
            using var connection = _db.OpenConnection();
            EnsureIncidentExists(connection, action.IncidentId);
            using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                UPDATE ActionItems SET
                  IncidentId = @incident, Description = @description, Owner = @owner,
                  TargetDate = @target, Status = @status, Category = @category, RootCauseCode = @rc
                WHERE Id = @id;
                """;
            Bind(cmd, action);
            cmd.Parameters.AddWithValue("@id", action.Id);
            if (cmd.ExecuteNonQuery() == 0) throw new NotFoundException("Action", action.Id);
        });
    }

    public void Delete(int actionId) =>
        SqlHelpers.Run("Deleting the action", () =>
        {
            using var connection = _db.OpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM ActionItems WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@id", actionId);
            if (cmd.ExecuteNonQuery() == 0) throw new NotFoundException("Action", actionId);
        });

    private static void EnsureIncidentExists(SqliteConnection connection, int incidentId)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Incidents WHERE Id = @id;";
        cmd.Parameters.AddWithValue("@id", incidentId);
        if (Convert.ToInt32(cmd.ExecuteScalar()) == 0) throw new NotFoundException("Incident", incidentId);
    }

    private static void Bind(SqliteCommand cmd, ActionItem a)
    {
        var p = cmd.Parameters;
        p.AddWithValue("@incident", a.IncidentId);
        p.AddWithValue("@description", a.Description.Trim());
        p.AddWithValue("@owner", a.Owner.Trim());
        p.AddWithValue("@target", SqlHelpers.ToDb(a.TargetDate));
        p.AddWithValue("@status", a.Status);
        p.AddWithValue("@category", SqlHelpers.ToDb(a.Category));
        p.AddWithValue("@rc", SqlHelpers.ToDb(a.RootCauseCode));
        p.AddWithValue("@origin", a.Origin);
    }

    private static List<ActionItem> ReadAll(SqliteCommand cmd)
    {
        var list = new List<ActionItem>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new ActionItem
            {
                Id = r.GetInt32(r.GetOrdinal("Id")),
                IncidentId = r.GetInt32(r.GetOrdinal("IncidentId")),
                Description = SqlHelpers.GetRequiredString(r, "Description"),
                Owner = SqlHelpers.GetRequiredString(r, "Owner"),
                TargetDate = SqlHelpers.GetDate(r, "TargetDate"),
                Status = SqlHelpers.GetRequiredString(r, "Status"),
                Category = SqlHelpers.GetString(r, "Category"),
                RootCauseCode = SqlHelpers.GetString(r, "RootCauseCode"),
                Origin = SqlHelpers.GetRequiredString(r, "Origin"),
                IncidentTitle = SqlHelpers.GetRequiredString(r, "IncidentTitle"),
                IncidentTag = SqlHelpers.GetRequiredString(r, "IncidentTag"),
            });
        }
        return list;
    }
}
