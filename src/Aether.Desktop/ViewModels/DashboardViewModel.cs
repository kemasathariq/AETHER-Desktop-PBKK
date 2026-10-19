using System.Collections.ObjectModel;
using System.Globalization;
using Aether.Core;
using Aether.Core.Models;
using Aether.Desktop.Common;

namespace Aether.Desktop.ViewModels;

public sealed class DashboardViewModel : PageViewModel
{
    private readonly AppServices _services;
    private bool _isLoading;
    private DashboardStats _stats = new();

    public DashboardViewModel(AppServices services) => _services = services;

    public override string Title => "Dashboard";

    public bool IsLoading { get => _isLoading; private set { Set(ref _isLoading, value); OnPropertyChanged(nameof(ShowContent)); } }
    public bool ShowContent => !IsLoading && !IsEmpty;
    public bool IsEmpty => _stats.IsEmpty;
    public bool ShowEmptyMessage => !IsLoading && IsEmpty;

    public string TotalIncidentsText { get; private set; } = "0";
    public string ActiveIncidentsText { get; private set; } = string.Empty;
    public string DowntimeText { get; private set; } = "0 h";
    public string DowntimeNote { get; private set; } = string.Empty;
    public string ActualLossText { get; private set; } = "0";
    public string LossNote { get; private set; } = string.Empty;
    public string ActionsText { get; private set; } = "0";
    public string ActionsNote { get; private set; } = string.Empty;

    public IReadOnlyList<LabelCount> ByStatus { get; private set; } = Array.Empty<LabelCount>();
    public IReadOnlyList<LabelCount> ByPlant { get; private set; } = Array.Empty<LabelCount>();
    public IReadOnlyList<LabelCount> ByMonth { get; private set; } = Array.Empty<LabelCount>();
    public ObservableCollection<Incident> Recent { get; } = new();

    public override async void Refresh()
    {
        IsLoading = true;
        OnPropertyChanged(nameof(ShowEmptyMessage));
        try
        {
            // The query runs off the UI thread; repositories open their own connection.
            var stats = await Task.Run(() => _services.Dashboard.GetStats());
            Apply(stats);
            ClearMessage();
        }
        catch (AetherException ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(ShowEmptyMessage));
        }
    }

    private void Apply(DashboardStats s)
    {
        _stats = s;
        var c = CultureInfo.CurrentCulture;

        TotalIncidentsText = s.TotalIncidents.ToString("N0", c);
        ActiveIncidentsText = $"{s.ActiveIncidents:N0} not closed or canceled";

        DowntimeText = s.TotalDowntimeHours.ToString("N1", c) + " h";
        DowntimeNote = s.IncidentsMissingDowntime == 0
            ? "Sum of downtime recorded on incidents."
            : $"Sum of recorded downtime. {s.IncidentsMissingDowntime:N0} incident(s) have no downtime recorded and are not counted.";

        ActualLossText = "k USD " + s.TotalActualLossKUsd.ToString("N0", c);
        LossNote = $"Recorded actual loss. Potential loss recorded separately: k USD {s.TotalPotentialLossKUsd:N0} (total k USD {s.TotalLossKUsd:N0})."
            + (s.IncidentsMissingLoss == 0 ? "" : $" {s.IncidentsMissingLoss:N0} incident(s) have no loss recorded.");

        var actionTotal = s.ActionsByStatus.Sum(a => a.Count);
        ActionsText = actionTotal.ToString("N0", c);
        ActionsNote = actionTotal == 0
            ? "No actions recorded yet."
            : string.Join("  ·  ", ActionStatus.All.Select(st => $"{st}: {s.ActionsByStatus.FirstOrDefault(a => a.Label == st)?.Count ?? 0}"));

        ByStatus = s.ByStatus;
        ByPlant = s.ByPlant;
        ByMonth = s.ByMonth.Select(m => new LabelCount(m.Month, m.Count)).TakeLast(12).ToList();

        Recent.Clear();
        foreach (var i in s.RecentIncidents) Recent.Add(i);

        foreach (var name in new[]
        {
            nameof(IsEmpty), nameof(ShowContent), nameof(TotalIncidentsText), nameof(ActiveIncidentsText),
            nameof(DowntimeText), nameof(DowntimeNote), nameof(ActualLossText), nameof(LossNote),
            nameof(ActionsText), nameof(ActionsNote), nameof(ByStatus), nameof(ByPlant), nameof(ByMonth),
        })
            OnPropertyChanged(name);
    }
}
