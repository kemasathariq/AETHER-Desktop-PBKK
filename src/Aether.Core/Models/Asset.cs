namespace Aether.Core.Models;

/// <summary>An asset from the source equipment master. The source covers 5 assets only.</summary>
public class Asset
{
    public int Id { get; set; }
    public string Tag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? EquipmentClass { get; set; }
    public string? PlantUnit { get; set; }
    public string? Discipline { get; set; }
    public string? Criticality { get; set; }
    public string? DesignLife { get; set; }
    public string? MonitoringMethod { get; set; }
    public string? LinkedArNo { get; set; }
    public string? DominantFailureMode { get; set; }

    /// <summary>Filled by queries that count records; 0 means no measurements are stored.</summary>
    public int RecordCount { get; set; }
}
