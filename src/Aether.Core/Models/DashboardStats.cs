namespace Aether.Core.Models;

public record LabelCount(string Label, int Count);

public record MonthlyPoint(string Month, int Count, double DowntimeHours, double ActualLossKUsd);

/// <summary>
/// Aggregates computed from the database. Sums skip null values; the "Missing" counts say how many
/// incidents had no value, so the numbers are never silently understated.
/// </summary>
public class DashboardStats
{
    public int TotalIncidents { get; init; }
    public int ActiveIncidents { get; init; }
    public double TotalDowntimeHours { get; init; }
    public double TotalActualLossKUsd { get; init; }
    public double TotalPotentialLossKUsd { get; init; }
    public double TotalLossKUsd => TotalActualLossKUsd + TotalPotentialLossKUsd;
    public int IncidentsMissingDowntime { get; init; }
    public int IncidentsMissingLoss { get; init; }
    public IReadOnlyList<LabelCount> ByStatus { get; init; } = Array.Empty<LabelCount>();
    public IReadOnlyList<LabelCount> ByPlant { get; init; } = Array.Empty<LabelCount>();
    public IReadOnlyList<MonthlyPoint> ByMonth { get; init; } = Array.Empty<MonthlyPoint>();
    public IReadOnlyList<LabelCount> ActionsByStatus { get; init; } = Array.Empty<LabelCount>();
    public IReadOnlyList<Incident> RecentIncidents { get; init; } = Array.Empty<Incident>();
    public bool IsEmpty => TotalIncidents == 0;
}
