using Aether.Core.Models;

namespace Aether.Core.Services;

/// <summary>
/// Rule-based incident prioritization. It is a deterministic scoring rule defined by the team
/// (see docs/DECISIONS.md D-07). It is not an AI model and does not identify root causes.
/// Source Pre-Risk is reported next to the result but is not part of the score.
/// </summary>
public static class PriorityService
{
    public const int ConditionWindowDays = 28;

    // Thresholds are round numbers chosen by the team, not values taken from the case data.
    public const double DowntimeHighHours = 24;
    public const double DowntimeMediumHours = 8;
    public const double LossHighKUsd = 1000;
    public const double LossMediumKUsd = 250;

    private const int DowntimeMax = 3;
    private const int LossMax = 3;
    private const int ConditionMax = 2;

    public static PriorityResult Evaluate(Incident incident, IReadOnlyList<ConditionRecord>? assetRecords)
    {
        var factors = new List<PriorityFactor>
        {
            ScoreDowntime(incident),
            ScoreLoss(incident),
            ScoreCondition(incident, assetRecords),
        };
        var notes = new List<string>();

        var scored = factors.Where(f => f.State == FactorState.Scored).ToList();
        var coreMissing = factors.Take(2).Any(f => f.State == FactorState.NotAvailable);
        var coreScored = factors.Take(2).Count(f => f.State == FactorState.Scored);

        var points = scored.Sum(f => f.Points);
        var scoredMax = scored.Sum(f => f.MaxPoints);
        var fullMax = factors.Sum(f => f.MaxPoints);

        if (coreScored == 0)
        {
            notes.Add("Neither downtime nor actual loss is recorded, so no priority is calculated.");
            return new PriorityResult
            {
                Category = PriorityCategory.NotRated,
                Points = 0,
                ScoredMaxPoints = 0,
                FullMaxPoints = fullMax,
                IsPartial = true,
                Factors = factors,
                Notes = notes,
                SourcePreRisk = incident.PreRisk,
            };
        }

        if (coreMissing)
            notes.Add("One consequence value is not recorded. It is not counted as zero; the result uses the recorded value only.");
        if (factors[2].State == FactorState.NotAvailable)
            notes.Add("The condition factor was not scored, so the result is based on consequence only.");
        notes.Add("The source Pre-Risk class is shown for reference and is not part of the score.");

        return new PriorityResult
        {
            Category = CategoryFor(points),
            Points = points,
            ScoredMaxPoints = scoredMax,
            FullMaxPoints = fullMax,
            IsPartial = coreMissing,
            Factors = factors,
            Notes = notes,
            SourcePreRisk = incident.PreRisk,
        };
    }

    /// <summary>Critical 6 and above, High 4-5, Medium 2-3, Low 0-1.</summary>
    public static PriorityCategory CategoryFor(int points) => points switch
    {
        >= 6 => PriorityCategory.Critical,
        >= 4 => PriorityCategory.High,
        >= 2 => PriorityCategory.Medium,
        _ => PriorityCategory.Low,
    };

    private static PriorityFactor ScoreDowntime(Incident i)
    {
        const string name = "Recorded downtime";
        if (i.DowntimeHours is not { } h)
            return new(name, FactorState.NotAvailable, 0, DowntimeMax, "Downtime is not recorded.");

        var (pts, band) = h switch
        {
            >= DowntimeHighHours => (3, $"{DowntimeHighHours:0} h or more"),
            >= DowntimeMediumHours => (2, $"{DowntimeMediumHours:0} h to under {DowntimeHighHours:0} h"),
            > 0 => (1, $"more than 0 h and under {DowntimeMediumHours:0} h"),
            _ => (0, "0 h recorded"),
        };
        return new(name, FactorState.Scored, pts, DowntimeMax, $"{h:0.##} h recorded ({band}).");
    }

    private static PriorityFactor ScoreLoss(Incident i)
    {
        const string name = "Recorded actual loss";
        if (i.ActualLossKUsd is not { } v)
            return new(name, FactorState.NotAvailable, 0, LossMax, "Actual loss is not recorded.");

        var (pts, band) = v switch
        {
            >= LossHighKUsd => (3, $"k USD {LossHighKUsd:0} or more"),
            >= LossMediumKUsd => (2, $"k USD {LossMediumKUsd:0} to under {LossHighKUsd:0}"),
            > 0 => (1, $"more than 0 and under k USD {LossMediumKUsd:0}"),
            _ => (0, "0 recorded"),
        };
        return new(name, FactorState.Scored, pts, LossMax, $"k USD {v:#,0.##} recorded ({band}).");
    }

    private static PriorityFactor ScoreCondition(Incident i, IReadOnlyList<ConditionRecord>? records)
    {
        const string name = "Asset condition vs. source limits";
        if (records is null || records.Count == 0)
            return new(name, FactorState.NotAvailable, 0, ConditionMax,
                "No condition measurements are stored for this asset.");
        if (i.OccurredOn is not { } occurred)
            return new(name, FactorState.NotAvailable, 0, ConditionMax,
                "The incident date is missing, so no measurement window can be chosen.");

        var from = occurred.Date.AddDays(-ConditionWindowDays);
        var window = records
            .Where(r => r.MeasuredOn.Date >= from && r.MeasuredOn.Date <= occurred.Date)
            .ToList();
        if (window.Count == 0)
            return new(name, FactorState.NotAvailable, 0, ConditionMax,
                $"No measurement was recorded in the {ConditionWindowDays} days up to the incident date.");

        var withLimits = window.Where(r => r.LimitDirection is "high" or "low"
                                           && (r.AlarmLimit is not null || r.TripLimit is not null)).ToList();
        if (withLimits.Count == 0)
            return new(name, FactorState.NotAvailable, 0, ConditionMax,
                "Measurements exist but no alarm or trip limit is recorded, so they cannot be compared.");

        var trips = withLimits.Where(r => Breaches(r, r.TripLimit)).Select(Describe).Distinct().ToList();
        if (trips.Count > 0)
            return new(name, FactorState.Scored, 2, ConditionMax,
                "Trip limit reached: " + string.Join("; ", trips) + ".");

        var alarms = withLimits.Where(r => Breaches(r, r.AlarmLimit)).Select(Describe).Distinct().ToList();
        if (alarms.Count > 0)
            return new(name, FactorState.Scored, 1, ConditionMax,
                "Alarm limit reached: " + string.Join("; ", alarms) + ".");

        return new(name, FactorState.Scored, 0, ConditionMax,
            $"{withLimits.Count} measurement(s) in the {ConditionWindowDays} days up to the incident, none at an alarm or trip limit.");
    }

    private static bool Breaches(ConditionRecord r, double? limit)
    {
        if (limit is null) return false;
        return r.LimitDirection == "high" ? r.Value >= limit : r.Value <= limit;
    }

    private static string Describe(ConditionRecord r) =>
        $"{r.Parameter} {r.Value:0.##}{(string.IsNullOrEmpty(r.Unit) ? "" : " " + r.Unit)} on {r.MeasuredOn:yyyy-MM-dd}";
}
