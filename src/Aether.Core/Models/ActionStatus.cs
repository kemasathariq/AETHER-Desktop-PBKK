namespace Aether.Core.Models;

/// <summary>Allowed action statuses. The stored text is the constant value.</summary>
public static class ActionStatus
{
    public const string Open = "Open";
    public const string InProgress = "In Progress";
    public const string Closed = "Closed";

    public static readonly IReadOnlyList<string> All = new[] { Open, InProgress, Closed };

    public static bool IsValid(string? status) => status is Open or InProgress or Closed;
}
