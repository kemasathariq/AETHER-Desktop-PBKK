using Aether.Core.Models;
using Aether.Core.Repositories;
using Aether.Infrastructure.Data;
using Microsoft.Data.Sqlite;

namespace Aether.Infrastructure.Repositories;

/// <summary>Computes dashboard aggregates with SQL so the numbers always follow the stored data.</summary>
public sealed class SqliteDashboardRepository : IDashboardRepository
{
    private readonly AetherDatabase _db;

    public SqliteDashboardRepository(AetherDatabase db) => _db = db;

    public DashboardStats GetStats(int recentCount = 8) =>
        SqlHelpers.Run("Calculating dashboard statistics", () =>
        {
            using var connection = _db.OpenConnection();

            int total, active, missingDowntime, missingLoss;
            double downtime, actual, potential;
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = """
                    SELECT COUNT(*),
                           COALESCE(SUM(CASE WHEN Status NOT IN ('RISK CLOSED','RISK CANCELED') THEN 1 ELSE 0 END), 0),
                           COALESCE(SUM(DowntimeHours), 0),
                           COALESCE(SUM(ActualLossKUsd), 0),
                           COALESCE(SUM(PotentialLossKUsd), 0),
                           COALESCE(SUM(CASE WHEN DowntimeHours IS NULL THEN 1 ELSE 0 END), 0),
                           COALESCE(SUM(CASE WHEN ActualLossKUsd IS NULL AND PotentialLossKUsd IS NULL THEN 1 ELSE 0 END), 0)
                    FROM Incidents;
                    """;
                using var r = cmd.ExecuteReader();
                r.Read();
                total = r.GetInt32(0);
                active = r.GetInt32(1);
                downtime = r.GetDouble(2);
                actual = r.GetDouble(3);
                potential = r.GetDouble(4);
                missingDowntime = r.GetInt32(5);
                missingLoss = r.GetInt32(6);
            }

            var byStatus = LabelCounts(connection, "SELECT Status, COUNT(*) FROM Incidents GROUP BY Status;")
                .OrderBy(x => IndexOf(IncidentStatuses.All, x.Label)).ToList();
            var byPlant = LabelCounts(connection,
                "SELECT Plant, COUNT(*) AS n FROM Incidents GROUP BY Plant ORDER BY n DESC, Plant LIMIT 8;");
            var actionsByStatus = LabelCounts(connection, "SELECT Status, COUNT(*) FROM ActionItems GROUP BY Status;")
                .OrderBy(x => IndexOf(ActionStatus.All, x.Label)).ToList();

            var byMonth = new List<MonthlyPoint>();
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = """
                    SELECT substr(OccurredOn, 1, 7) AS Month, COUNT(*),
                           COALESCE(SUM(DowntimeHours), 0), COALESCE(SUM(ActualLossKUsd), 0)
                    FROM Incidents
                    WHERE OccurredOn IS NOT NULL AND length(OccurredOn) >= 7
                    GROUP BY Month ORDER BY Month;
                    """;
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    byMonth.Add(new MonthlyPoint(r.GetString(0), r.GetInt32(1), r.GetDouble(2), r.GetDouble(3)));
            }

            IReadOnlyList<Incident> recent;
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = """
                    SELECT Id, SourceSerialNo, Reference, ArNo, Title, Plant, TagNumber, EquipmentClass, EquipmentType,
                           Discipline, Component, FailureMechanism, HighestImpact, PreRisk, RiskScore, PicRca, Status,
                           OccurredOn, RcaDueDate, DowntimeHours, ActualLossKUsd, PotentialLossKUsd, Origin
                    FROM Incidents ORDER BY OccurredOn DESC, Id DESC LIMIT @n;
                    """;
                cmd.Parameters.AddWithValue("@n", Math.Max(0, recentCount));
                recent = SqliteIncidentRepository.ReadAll(cmd);
            }

            return new DashboardStats
            {
                TotalIncidents = total,
                ActiveIncidents = active,
                TotalDowntimeHours = downtime,
                TotalActualLossKUsd = actual,
                TotalPotentialLossKUsd = potential,
                IncidentsMissingDowntime = missingDowntime,
                IncidentsMissingLoss = missingLoss,
                ByStatus = byStatus,
                ByPlant = byPlant,
                ByMonth = byMonth,
                ActionsByStatus = actionsByStatus,
                RecentIncidents = recent,
            };
        });

    private static int IndexOf(IReadOnlyList<string> order, string label)
    {
        for (var i = 0; i < order.Count; i++)
            if (order[i] == label) return i;
        return order.Count;
    }

    private static List<LabelCount> LabelCounts(SqliteConnection connection, string sql)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        var list = new List<LabelCount>();
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(new LabelCount(r.GetString(0), r.GetInt32(1)));
        return list;
    }
}
