namespace Aether.Core.Models;

/// <summary>One weekly measurement of one parameter of one asset.</summary>
public class ConditionRecord
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public int WeekNo { get; set; }
    public DateTime MeasuredOn { get; set; }
    public string Parameter { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public double Value { get; set; }

    /// <summary>Limits come from the source (hero case limits). Null when no limit is recorded.</summary>
    public double? AlarmLimit { get; set; }
    public double? TripLimit { get; set; }

    /// <summary>"high" (breach when value rises to the limit) or "low" (breach when it falls to it).</summary>
    public string? LimitDirection { get; set; }

    /// <summary>Weekly Health Status of the asset from the source (NORMAL, ALARM, TRIP).</summary>
    public string? HealthStatus { get; set; }
    public string? Remark { get; set; }
}
