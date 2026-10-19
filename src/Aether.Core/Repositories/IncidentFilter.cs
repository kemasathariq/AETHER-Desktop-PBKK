namespace Aether.Core.Repositories;

/// <summary>Filters for the incident list. Null or empty values mean "no filter".</summary>
public class IncidentFilter
{
    /// <summary>Matches title, tag, reference, AR number, plant, component and failure mechanism.</summary>
    public string? Text { get; set; }
    public string? Status { get; set; }
    public string? Plant { get; set; }
    public string? Discipline { get; set; }

    /// <summary>When true, closed and canceled incidents are hidden.</summary>
    public bool ActiveOnly { get; set; }
}

public class ActionFilter
{
    public string? Text { get; set; }
    public string? Status { get; set; }
    public int? IncidentId { get; set; }
}
