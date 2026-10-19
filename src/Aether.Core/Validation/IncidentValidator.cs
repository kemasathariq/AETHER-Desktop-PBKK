using Aether.Core.Models;

namespace Aether.Core.Validation;

public static class IncidentValidator
{
    public const int MaxTitleLength = 200;

    public static ValidationResult Validate(Incident incident)
    {
        var r = new ValidationResult();

        if (string.IsNullOrWhiteSpace(incident.Title))
            r.Add(nameof(Incident.Title), "Title is required.");
        else if (incident.Title.Length > MaxTitleLength)
            r.Add(nameof(Incident.Title), $"Title must be at most {MaxTitleLength} characters.");

        if (string.IsNullOrWhiteSpace(incident.Plant))
            r.Add(nameof(Incident.Plant), "Plant is required.");

        if (string.IsNullOrWhiteSpace(incident.TagNumber))
            r.Add(nameof(Incident.TagNumber), "Asset tag is required.");

        if (incident.OccurredOn is null)
            r.Add(nameof(Incident.OccurredOn), "Date of occurrence is required.");

        if (!IncidentStatuses.All.Contains(incident.Status))
            r.Add(nameof(Incident.Status), "Choose one of the listed statuses.");

        CheckNonNegative(r, nameof(Incident.DowntimeHours), "Downtime (hours)", incident.DowntimeHours);
        CheckNonNegative(r, nameof(Incident.ActualLossKUsd), "Actual loss", incident.ActualLossKUsd);
        CheckNonNegative(r, nameof(Incident.PotentialLossKUsd), "Potential loss", incident.PotentialLossKUsd);
        CheckNonNegative(r, nameof(Incident.RiskScore), "Risk score", incident.RiskScore);

        return r;
    }

    private static void CheckNonNegative(ValidationResult r, string field, string label, double? value)
    {
        if (value is null) return;
        if (!double.IsFinite(value.Value))
            r.Add(field, $"{label} must be a number.");
        else if (value < 0)
            r.Add(field, $"{label} cannot be negative.");
    }
}
