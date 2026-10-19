namespace Aether.Core.Models;

/// <summary>
/// One incident. Numeric fields are nullable: null means "not recorded" and is never treated as zero.
/// Field meaning and the source column of each property are listed in docs/DATA_DICTIONARY.md.
/// </summary>
public class Incident
{
    public int Id { get; set; }

    /// <summary>serial_no from the source register; null for incidents entered in the app.</summary>
    public int? SourceSerialNo { get; set; }

    /// <summary>MTO number (mto_no). Not unique in the source data.</summary>
    public string? Reference { get; set; }
    public string? ArNo { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Plant { get; set; } = string.Empty;
    public string TagNumber { get; set; } = string.Empty;
    public string? EquipmentClass { get; set; }
    public string? EquipmentType { get; set; }
    public string? Discipline { get; set; }
    public string? Component { get; set; }
    public string? FailureMechanism { get; set; }
    public string? HighestImpact { get; set; }

    /// <summary>Source-provided Pre-Risk class (I-IV). Not a calculated priority.</summary>
    public string? PreRisk { get; set; }
    public double? RiskScore { get; set; }
    public string? PicRca { get; set; }
    public string Status { get; set; } = IncidentStatuses.NewRegistered;
    public DateTime? OccurredOn { get; set; }
    public DateTime? RcaDueDate { get; set; }
    public double? DowntimeHours { get; set; }
    public double? ActualLossKUsd { get; set; }
    public double? PotentialLossKUsd { get; set; }
    public string Origin { get; set; } = DataOrigin.User;

    /// <summary>Derived: actual + potential loss (k USD). Null when neither is recorded.</summary>
    public double? TotalLossKUsd =>
        ActualLossKUsd is null && PotentialLossKUsd is null
            ? null
            : (ActualLossKUsd ?? 0) + (PotentialLossKUsd ?? 0);

    public bool IsActive => IncidentStatuses.IsActive(Status);
}
