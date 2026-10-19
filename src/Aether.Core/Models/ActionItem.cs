namespace Aether.Core.Models;

public class ActionItem
{
    public int Id { get; set; }
    public int IncidentId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public DateTime? TargetDate { get; set; }
    public string Status { get; set; } = ActionStatus.Open;

    /// <summary>Corrective or Proactive for actions imported from the RCA data; null for others.</summary>
    public string? Category { get; set; }
    public string? RootCauseCode { get; set; }
    public string Origin { get; set; } = DataOrigin.User;

    // Read-only display fields filled by queries that join the incident.
    public string IncidentTitle { get; set; } = string.Empty;
    public string IncidentTag { get; set; } = string.Empty;
}
