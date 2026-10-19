using System.Collections.ObjectModel;
using Aether.Core;
using Aether.Core.Models;
using Aether.Desktop.Common;
using Aether.Desktop.Controls;

namespace Aether.Desktop.ViewModels;

public sealed class AssetsViewModel : PageViewModel
{
    private readonly AppServices _services;
    private Asset? _selectedAsset;
    private string? _selectedParameter;
    private List<ConditionRecord> _records = new();

    public AssetsViewModel(AppServices services) => _services = services;

    public override string Title => "Asset Monitoring";

    public ObservableCollection<Asset> Assets { get; } = new();
    public ObservableCollection<string> Parameters { get; } = new();
    public ObservableCollection<ConditionRecord> Records { get; } = new();

    public Asset? SelectedAsset
    {
        get => _selectedAsset;
        set
        {
            if (!Set(ref _selectedAsset, value)) return;
            LoadAsset();
        }
    }

    public string? SelectedParameter
    {
        get => _selectedParameter;
        set { if (Set(ref _selectedParameter, value)) ShowParameter(); }
    }

    public bool HasAssets => Assets.Count > 0;
    public bool HasSelection => SelectedAsset is not null;
    public IReadOnlyList<DetailRow> Details { get; private set; } = Array.Empty<DetailRow>();
    public string CoverageNote { get; private set; } = "";
    public string EmptyMessage { get; private set; } = "";
    public string ChartMessage { get; private set; } = "";
    public bool ShowChart { get; private set; }
    public IReadOnlyList<ChartPoint> ChartPoints { get; private set; } = Array.Empty<ChartPoint>();
    public double? AlarmLimit { get; private set; }
    public double? TripLimit { get; private set; }
    public string Unit { get; private set; } = "";
    public string LimitText { get; private set; } = "";
    public string RecordSummary { get; private set; } = "";

    public override void Refresh()
    {
        ClearMessage();
        var keepId = SelectedAsset?.Id;
        try
        {
            var assets = _services.Assets.GetAssets();
            Assets.Clear();
            foreach (var a in assets) Assets.Add(a);
        }
        catch (AetherException ex)
        {
            ShowError(ex.Message);
        }

        EmptyMessage = Assets.Count == 0
            ? "No assets or condition measurements are stored in the database. The supplied case data includes weekly measurements for 5 assets only."
            : "";
        CoverageNote = Assets.Count == 0 ? "" :
            $"{Assets.Count} asset(s) have weekly measurements in the supplied case data. All other assets in the incident register have no condition data. These are historical weekly records, not live monitoring.";
        OnPropertyChanged(nameof(EmptyMessage));
        OnPropertyChanged(nameof(CoverageNote));
        OnPropertyChanged(nameof(HasAssets));

        SelectedAsset = keepId is null ? Assets.FirstOrDefault() : Assets.FirstOrDefault(a => a.Id == keepId) ?? Assets.FirstOrDefault();
    }

    private void LoadAsset()
    {
        OnPropertyChanged(nameof(HasSelection));
        Parameters.Clear();
        _records = new();
        var a = SelectedAsset;

        if (a is null)
        {
            Details = Array.Empty<DetailRow>();
            OnPropertyChanged(nameof(Details));
            ShowParameter();
            return;
        }

        Details = new[]
        {
            DetailRow.Of("Name", a.Name),
            DetailRow.Of("Type", a.Type),
            DetailRow.Of("Equipment class", a.EquipmentClass),
            DetailRow.Of("Plant / unit", a.PlantUnit),
            DetailRow.Of("Discipline", a.Discipline),
            DetailRow.Of("Criticality", a.Criticality),
            DetailRow.Of("Design life", a.DesignLife),
            DetailRow.Of("Monitoring method", a.MonitoringMethod),
            DetailRow.Of("Linked AR number", a.LinkedArNo),
            DetailRow.Of("Dominant failure mode", a.DominantFailureMode),
        };
        OnPropertyChanged(nameof(Details));

        try { _records = _services.Assets.GetRecords(a.Id).ToList(); }
        catch (AetherException ex) { ShowError(ex.Message); }

        foreach (var p in _records.Select(r => r.Parameter).Distinct()) Parameters.Add(p);
        _selectedParameter = Parameters.FirstOrDefault();
        OnPropertyChanged(nameof(SelectedParameter));
        ShowParameter();
    }

    private void ShowParameter()
    {
        Records.Clear();
        var rows = _records.Where(r => r.Parameter == _selectedParameter).OrderBy(r => r.MeasuredOn).ToList();
        foreach (var r in rows.AsEnumerable().Reverse()) Records.Add(r);

        ChartPoints = rows.Select(r => new ChartPoint(r.MeasuredOn, r.Value)).ToList();
        var first = rows.FirstOrDefault();
        AlarmLimit = first?.AlarmLimit;
        TripLimit = first?.TripLimit;
        Unit = first?.Unit ?? "";
        ShowChart = rows.Count >= 2;

        if (SelectedAsset is null)
            ChartMessage = "Select an asset to see its measurements.";
        else if (_records.Count == 0)
            ChartMessage = "No condition measurements are stored for this asset, so there is nothing to chart.";
        else if (rows.Count < 2)
            ChartMessage = "At least two measurements are needed to draw a history chart for this parameter.";
        else
            ChartMessage = "";

        LimitText = first is null ? "" :
            (first.AlarmLimit is null && first.TripLimit is null
                ? "No alarm or trip limit is recorded for this parameter."
                : $"Source limits ({first.LimitDirection ?? "direction not recorded"}): alarm {Fmt(first.AlarmLimit)}, trip {Fmt(first.TripLimit)}{(string.IsNullOrEmpty(first.Unit) ? "" : " " + first.Unit)}.");

        RecordSummary = rows.Count == 0 ? "" :
            $"{rows.Count} weekly measurement(s), {rows[0].MeasuredOn:d MMM yyyy} to {rows[^1].MeasuredOn:d MMM yyyy}.";

        foreach (var n in new[]
        {
            nameof(ShowChart), nameof(ChartPoints), nameof(AlarmLimit), nameof(TripLimit), nameof(Unit),
            nameof(ChartMessage), nameof(LimitText), nameof(RecordSummary),
        })
            OnPropertyChanged(n);
    }

    private static string Fmt(double? v) => v is null ? "not recorded" : v.Value.ToString("0.##");
}
