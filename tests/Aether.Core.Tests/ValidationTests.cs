using Aether.Core.Models;
using Aether.Core.Validation;
using Xunit;

namespace Aether.Core.Tests;

public class ValidationTests
{
    private static Incident ValidIncident() => new()
    {
        Title = "Seal leak", Plant = "ARP", TagNumber = "PU-2101B",
        Status = IncidentStatuses.RcaProcess, OccurredOn = new DateTime(2026, 3, 12),
    };

    [Fact]
    public void ValidIncident_HasNoErrors() =>
        Assert.True(IncidentValidator.Validate(ValidIncident()).IsValid);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankRequiredText_IsRejected(string blank)
    {
        var i = ValidIncident();
        i.Title = blank; i.Plant = blank; i.TagNumber = blank;
        var r = IncidentValidator.Validate(i);
        Assert.Contains(nameof(Incident.Title), r.Errors.Keys);
        Assert.Contains(nameof(Incident.Plant), r.Errors.Keys);
        Assert.Contains(nameof(Incident.TagNumber), r.Errors.Keys);
    }

    [Fact]
    public void MissingDate_AndUnknownStatus_AreRejected()
    {
        var i = ValidIncident();
        i.OccurredOn = null; i.Status = "DONE";
        var r = IncidentValidator.Validate(i);
        Assert.Contains(nameof(Incident.OccurredOn), r.Errors.Keys);
        Assert.Contains(nameof(Incident.Status), r.Errors.Keys);
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidNumbers_AreRejected(double bad)
    {
        var i = ValidIncident();
        i.DowntimeHours = bad; i.ActualLossKUsd = bad; i.PotentialLossKUsd = bad;
        Assert.Equal(3, IncidentValidator.Validate(i).Errors.Count);
    }

    [Fact]
    public void NullNumbers_AreAllowed_AndMeanNotRecorded()
    {
        var i = ValidIncident();
        Assert.True(IncidentValidator.Validate(i).IsValid);
        Assert.Null(i.TotalLossKUsd);
    }

    [Fact]
    public void TotalLoss_IsSumOfRecordedParts()
    {
        var i = ValidIncident();
        i.ActualLossKUsd = 100; i.PotentialLossKUsd = null;
        Assert.Equal(100, i.TotalLossKUsd);
        i.PotentialLossKUsd = 25.5;
        Assert.Equal(125.5, i.TotalLossKUsd);
    }

    [Fact]
    public void ThrowIfInvalid_RaisesValidationException()
    {
        var i = ValidIncident(); i.Title = "";
        Assert.Throws<ValidationException>(() => IncidentValidator.Validate(i).ThrowIfInvalid());
    }

    private static ActionItem ValidAction() => new()
    {
        IncidentId = 1, Description = "Replace seal", Owner = "REL-05",
        TargetDate = new DateTime(2026, 6, 1), Status = ActionStatus.Open,
    };

    [Fact]
    public void ValidAction_HasNoErrors() =>
        Assert.True(ActionValidator.Validate(ValidAction()).IsValid);

    [Fact]
    public void Action_RequiredFields_AreChecked()
    {
        var a = ValidAction();
        a.Description = " "; a.Owner = ""; a.IncidentId = 0; a.TargetDate = null; a.Status = "Done";
        Assert.Equal(5, ActionValidator.Validate(a).Errors.Count);
    }

    [Theory]
    [InlineData("Open", true)]
    [InlineData("In Progress", true)]
    [InlineData("Closed", true)]
    [InlineData("open", false)]
    [InlineData(null, false)]
    public void ActionStatus_AcceptsOnlyDefinedValues(string? status, bool expected) =>
        Assert.Equal(expected, ActionStatus.IsValid(status));

    [Theory]
    [InlineData("", true, null)]
    [InlineData("  ", true, null)]
    [InlineData("12.5", true, 12.5)]
    [InlineData("12,5", true, 12.5)]
    [InlineData("0", true, 0.0)]
    [InlineData("abc", false, null)]
    [InlineData("1,584.5", false, null)]
    [InlineData("NaN", false, null)]
    [InlineData("Infinity", false, null)]
    public void NumberParser_HandlesTypedText(string text, bool ok, double? expected)
    {
        Assert.Equal(ok, NumberParser.TryParseOptional(text, out var value));
        Assert.Equal(expected, value);
    }
}
