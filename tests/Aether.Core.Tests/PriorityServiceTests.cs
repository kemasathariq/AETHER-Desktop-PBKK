using Aether.Core.Models;
using Aether.Core.Services;
using Xunit;

namespace Aether.Core.Tests;

public class PriorityServiceTests
{
    private static Incident Make(double? downtime, double? actualLoss, string? preRisk = "IV") => new()
    {
        Title = "t", Plant = "P", TagNumber = "X-1", Status = IncidentStatuses.RcaProcess,
        OccurredOn = new DateTime(2026, 4, 29), DowntimeHours = downtime, ActualLossKUsd = actualLoss, PreRisk = preRisk,
    };

    private static ConditionRecord Rec(string date, double value, double? alarm, double? trip, string dir = "high") => new()
    {
        MeasuredOn = DateTime.Parse(date), Parameter = "Vibration", Unit = "micron", Value = value,
        AlarmLimit = alarm, TripLimit = trip, LimitDirection = dir,
    };

    [Theory]
    [InlineData(0, 0, PriorityCategory.Low, 0)]
    [InlineData(0.5, 10, PriorityCategory.Medium, 2)]   // 1 + 1
    [InlineData(8, 250, PriorityCategory.High, 4)]      // 2 + 2 (boundaries are inclusive)
    [InlineData(24, 1000, PriorityCategory.Critical, 6)]
    [InlineData(32, 1584, PriorityCategory.Critical, 6)]
    [InlineData(23.9, 999.9, PriorityCategory.High, 4)] // 2 + 2
    public void ConsequenceFactors_MapToCategory(double downtime, double loss, PriorityCategory cat, int points)
    {
        var r = PriorityService.Evaluate(Make(downtime, loss), null);
        Assert.Equal(cat, r.Category);
        Assert.Equal(points, r.Points);
        Assert.False(r.IsPartial is false && r.Factors.Count != 3);
    }

    [Fact]
    public void MissingDowntime_IsNotTreatedAsZero_AndIsFlagged()
    {
        var r = PriorityService.Evaluate(Make(null, 1200), null);
        Assert.True(r.IsPartial);
        Assert.Equal(FactorState.NotAvailable, r.Factors[0].State);
        Assert.Equal(3, r.Points);
        Assert.Equal(PriorityCategory.Medium, r.Category);
        Assert.Contains(r.Notes, n => n.Contains("not counted as zero"));
    }

    [Fact]
    public void NoConsequenceData_IsNotRated()
    {
        var r = PriorityService.Evaluate(Make(null, null), null);
        Assert.Equal(PriorityCategory.NotRated, r.Category);
        Assert.True(r.IsPartial);
    }

    [Fact]
    public void PreRisk_DoesNotChangeTheScore()
    {
        var a = PriorityService.Evaluate(Make(10, 300, "I"), null);
        var b = PriorityService.Evaluate(Make(10, 300, "IV"), null);
        Assert.Equal(a.Points, b.Points);
        Assert.Equal("I", a.SourcePreRisk);
        Assert.Equal("IV", b.SourcePreRisk);
    }

    [Fact]
    public void NoAssetRecords_ConditionFactorIsNotAvailable_NotZero()
    {
        var f = PriorityService.Evaluate(Make(5, 100), new List<ConditionRecord>()).Factors[2];
        Assert.Equal(FactorState.NotAvailable, f.State);
    }

    [Fact]
    public void TripBreachInWindow_AddsTwoPoints()
    {
        var recs = new[] { Rec("2026-04-22", 71.7, 45, 75), Rec("2026-04-29", 80, 45, 75) };
        var r = PriorityService.Evaluate(Make(0, 0), recs);
        Assert.Equal(2, r.Factors[2].Points);
        Assert.Equal(2, r.Points);
        Assert.Contains("Trip limit", r.Factors[2].Basis);
    }

    [Fact]
    public void AlarmOnlyBreach_AddsOnePoint()
    {
        var r = PriorityService.Evaluate(Make(0, 0), new[] { Rec("2026-04-22", 60, 45, 75) });
        Assert.Equal(1, r.Factors[2].Points);
        Assert.Contains("Alarm limit", r.Factors[2].Basis);
    }

    [Fact]
    public void LowDirectionLimits_BreachWhenValueFalls()
    {
        var recs = new[] { Rec("2026-04-22", 1.3, 1.4, 1.1, "low") };
        Assert.Equal(1, PriorityService.Evaluate(Make(0, 0), recs).Factors[2].Points);
        var trip = new[] { Rec("2026-04-22", 1.0, 1.4, 1.1, "low") };
        Assert.Equal(2, PriorityService.Evaluate(Make(0, 0), trip).Factors[2].Points);
    }

    [Fact]
    public void WithinLimits_ScoresZeroButIsScored()
    {
        var f = PriorityService.Evaluate(Make(0, 0), new[] { Rec("2026-04-22", 30, 45, 75) }).Factors[2];
        Assert.Equal(FactorState.Scored, f.State);
        Assert.Equal(0, f.Points);
    }

    [Fact]
    public void RecordsOutsideWindow_AreIgnored()
    {
        var recs = new[]
        {
            Rec("2026-03-01", 90, 45, 75),   // too early
            Rec("2026-05-06", 90, 45, 75),   // after the incident
        };
        var f = PriorityService.Evaluate(Make(0, 0), recs).Factors[2];
        Assert.Equal(FactorState.NotAvailable, f.State);
    }

    [Fact]
    public void RecordsWithoutLimits_CannotBeCompared()
    {
        var rec = new ConditionRecord { MeasuredOn = new DateTime(2026, 4, 22), Parameter = "X", Value = 5 };
        var f = PriorityService.Evaluate(Make(0, 0), new[] { rec }).Factors[2];
        Assert.Equal(FactorState.NotAvailable, f.State);
    }

    [Theory]
    [InlineData(null, "Vibration")]
    [InlineData("", "Vibration")]
    [InlineData("micron", "")]
    public void RecordsWithoutUnitOrParameter_CannotBeCompared(string? unit, string parameter)
    {
        var record = Rec("2026-04-22", 90, 45, 75);
        record.Unit = unit;
        record.Parameter = parameter;
        var factor = PriorityService.Evaluate(Make(0, 0), new[] { record }).Factors[2];
        Assert.Equal(FactorState.NotAvailable, factor.State);
    }

    [Fact]
    public void SameInput_GivesSameResult()
    {
        var a = PriorityService.Evaluate(Make(12, 400), null);
        var b = PriorityService.Evaluate(Make(12, 400), null);
        Assert.Equal(a.Points, b.Points);
        Assert.Equal(a.Category, b.Category);
    }

    [Theory]
    [InlineData(0, PriorityCategory.Low)]
    [InlineData(1, PriorityCategory.Low)]
    [InlineData(2, PriorityCategory.Medium)]
    [InlineData(3, PriorityCategory.Medium)]
    [InlineData(4, PriorityCategory.High)]
    [InlineData(5, PriorityCategory.High)]
    [InlineData(6, PriorityCategory.Critical)]
    [InlineData(8, PriorityCategory.Critical)]
    public void CategoryThresholds(int points, PriorityCategory expected) =>
        Assert.Equal(expected, PriorityService.CategoryFor(points));
}
