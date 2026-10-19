using System.Collections.ObjectModel;
using Aether.Core;
using Aether.Core.Models;
using Aether.Core.Services;
using Aether.Desktop.Common;

namespace Aether.Desktop.ViewModels;

public sealed class PriorityRow
{
    public PriorityRow(Incident incident, PriorityResult result)
    {
        Incident = incident;
        Result = result;
    }

    public Incident Incident { get; }
    public PriorityResult Result { get; }
    public PriorityCategory Category => Result.Category;
    public string CategoryText => Result.Category == PriorityCategory.NotRated ? "Not rated" : Result.Category.ToString();
    public string PointsText => Result.Category == PriorityCategory.NotRated ? "—" : $"{Result.Points} / {Result.ScoredMaxPoints}";
    public string PartialText => Result.IsPartial ? "Partial data" : "";
}

public sealed class PriorityViewModel : PageViewModel
{
    public const string AllCategories = "All categories";

    private readonly AppServices _services;
    private List<PriorityRow> _all = new();
    private string _searchText = "";
    private string _categoryFilter = AllCategories;
    private bool _activeOnly = true;
    private PriorityRow? _selected;

    public PriorityViewModel(AppServices services) => _services = services;

    public override string Title => "Incident Priority";

    public ObservableCollection<PriorityRow> Rows { get; } = new();
    public IReadOnlyList<string> CategoryOptions { get; } =
        new[] { AllCategories, "Critical", "High", "Medium", "Low", "Not rated" };

    public string SearchText { get => _searchText; set { if (Set(ref _searchText, value)) ApplyFilter(); } }
    public string CategoryFilter { get => _categoryFilter; set { if (Set(ref _categoryFilter, value ?? AllCategories)) ApplyFilter(); } }
    public bool ActiveOnly { get => _activeOnly; set { if (Set(ref _activeOnly, value)) ApplyFilter(); } }

    public PriorityRow? Selected
    {
        get => _selected;
        set
        {
            if (!Set(ref _selected, value)) return;
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(Factors));
            OnPropertyChanged(nameof(Notes));
            OnPropertyChanged(nameof(Facts));
            OnPropertyChanged(nameof(SourceRiskText));
            OnPropertyChanged(nameof(ConditionNote));
        }
    }

    public bool HasSelection => Selected is not null;
    public IReadOnlyList<PriorityFactor> Factors => Selected?.Result.Factors ?? Array.Empty<PriorityFactor>();
    public IReadOnlyList<string> Notes => Selected?.Result.Notes ?? Array.Empty<string>();
    public string EmptyMessage { get; private set; } = "";
    public bool IsListEmpty => Rows.Count == 0;
    public string CountText { get; private set; } = "";

    public string SourceRiskText => Selected?.Incident.PreRisk is { Length: > 0 } p
        ? $"Pre-Risk {p} (recorded in the source). This is shown for reference and is not part of the calculated priority."
        : "No Pre-Risk class is recorded for this incident.";

    public string ConditionNote => Selected is null ? "" :
        Selected.Result.Factors.Count > 2 && Selected.Result.Factors[2].State == FactorState.NotAvailable
            ? "Condition data is only available for 5 assets in the supplied case data."
            : "";

    public IReadOnlyList<DetailRow> Facts
    {
        get
        {
            var i = Selected?.Incident;
            if (i is null) return Array.Empty<DetailRow>();
            return new[]
            {
                DetailRow.Of("Asset tag", i.TagNumber),
                DetailRow.Of("Plant", i.Plant),
                DetailRow.Of("Status", i.Status),
                DetailRow.Of("Date of occurrence", i.OccurredOn),
                DetailRow.Of("Failure mechanism", i.FailureMechanism),
                DetailRow.Of("Downtime", i.DowntimeHours, "h"),
                DetailRow.Of("Actual loss", i.ActualLossKUsd, "k USD"),
                DetailRow.Of("Potential loss", i.PotentialLossKUsd, "k USD"),
            };
        }
    }

    public override void Refresh()
    {
        ClearMessage();
        var keepId = Selected?.Incident.Id;
        try
        {
            // Condition records exist only for the few assets in the source; load each once.
            var recordsByTag = _services.Assets.GetAssets()
                .ToDictionary(a => a.Tag, a => _services.Assets.GetRecords(a.Id), StringComparer.OrdinalIgnoreCase);

            _all = _services.Incidents.Search()
                .Select(i =>
                {
                    recordsByTag.TryGetValue(i.TagNumber, out var records);
                    return new PriorityRow(i, PriorityService.Evaluate(i, records));
                })
                .OrderByDescending(r => r.Result.Points)
                .ThenByDescending(r => r.Incident.ActualLossKUsd ?? -1)
                .ThenByDescending(r => r.Incident.OccurredOn)
                .ToList();
        }
        catch (AetherException ex)
        {
            ShowError(ex.Message);
            _all = new();
        }
        ApplyFilter(keepId);
    }

    private void ApplyFilter(int? keepId = null)
    {
        keepId ??= Selected?.Incident.Id;
        IEnumerable<PriorityRow> q = _all;
        if (ActiveOnly) q = q.Where(r => r.Incident.IsActive);
        if (CategoryFilter != AllCategories) q = q.Where(r => r.CategoryText == CategoryFilter);
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var t = SearchText.Trim();
            q = q.Where(r => Contains(r.Incident.Title, t) || Contains(r.Incident.TagNumber, t)
                             || Contains(r.Incident.Plant, t) || Contains(r.Incident.Reference, t));
        }

        Rows.Clear();
        foreach (var r in q) Rows.Add(r);
        Selected = keepId is null ? null : Rows.FirstOrDefault(r => r.Incident.Id == keepId);

        EmptyMessage = Rows.Count > 0 ? ""
            : _all.Count == 0 ? "There are no incidents to prioritize yet."
            : "No incidents match the current filters.";
        CountText = $"{Rows.Count:N0} incident(s) shown";
        OnPropertyChanged(nameof(EmptyMessage));
        OnPropertyChanged(nameof(IsListEmpty));
        OnPropertyChanged(nameof(CountText));
    }

    private static bool Contains(string? text, string term) =>
        text?.Contains(term, StringComparison.OrdinalIgnoreCase) == true;
}
