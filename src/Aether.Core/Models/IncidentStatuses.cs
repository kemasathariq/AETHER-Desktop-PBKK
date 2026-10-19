namespace Aether.Core.Models;

/// <summary>Incident status values exactly as they appear in the source register (overall_status).</summary>
public static class IncidentStatuses
{
    public const string NewRegistered = "NEW REGISTERED";
    public const string RcaProcess = "RCA PROCESS";
    public const string CaPaExecution = "CA/PA EXECUTION";
    public const string MonitoringResult = "MONITORING RESULT";
    public const string RiskClosed = "RISK CLOSED";
    public const string RiskCanceled = "RISK CANCELED";

    public static readonly IReadOnlyList<string> All = new[]
    {
        NewRegistered, RcaProcess, CaPaExecution, MonitoringResult, RiskClosed, RiskCanceled,
    };

    /// <summary>True when the incident is still being worked on (not closed or canceled).</summary>
    public static bool IsActive(string? status) =>
        status != RiskClosed && status != RiskCanceled;
}
