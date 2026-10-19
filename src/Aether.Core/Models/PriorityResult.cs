namespace Aether.Core.Models;

public enum PriorityCategory { NotRated, Low, Medium, High, Critical }

public enum FactorState
{
    /// <summary>The factor was scored from recorded data.</summary>
    Scored,
    /// <summary>The needed data is missing, so the factor was not scored (not treated as zero).</summary>
    NotAvailable,
}

public record PriorityFactor(string Name, FactorState State, int Points, int MaxPoints, string Basis);

/// <summary>
/// Output of the rule-based prioritization (docs/DECISIONS.md D-07). Calculated by this app,
/// separate from the source Pre-Risk class.
/// </summary>
public class PriorityResult
{
    public PriorityCategory Category { get; init; }
    public int Points { get; init; }
    public int ScoredMaxPoints { get; init; }
    public int FullMaxPoints { get; init; }
    public bool IsPartial { get; init; }
    public IReadOnlyList<PriorityFactor> Factors { get; init; } = Array.Empty<PriorityFactor>();
    public IReadOnlyList<string> Notes { get; init; } = Array.Empty<string>();
    public string? SourcePreRisk { get; init; }
}
