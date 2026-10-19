using Aether.Core.Models;

namespace Aether.Core.Validation;

public static class ActionValidator
{
    public const int MaxDescriptionLength = 500;

    public static ValidationResult Validate(ActionItem action)
    {
        var r = new ValidationResult();

        if (string.IsNullOrWhiteSpace(action.Description))
            r.Add(nameof(ActionItem.Description), "Description is required.");
        else if (action.Description.Length > MaxDescriptionLength)
            r.Add(nameof(ActionItem.Description), $"Description must be at most {MaxDescriptionLength} characters.");

        if (action.IncidentId <= 0)
            r.Add(nameof(ActionItem.IncidentId), "Choose the related incident.");

        if (string.IsNullOrWhiteSpace(action.Owner))
            r.Add(nameof(ActionItem.Owner), "Owner is required.");

        if (action.TargetDate is null)
            r.Add(nameof(ActionItem.TargetDate), "Target date is required.");

        if (!ActionStatus.IsValid(action.Status))
            r.Add(nameof(ActionItem.Status), "Status must be Open, In Progress or Closed.");

        return r;
    }
}
