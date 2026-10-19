using System.Collections.ObjectModel;
using Aether.Core;
using Aether.Core.Models;
using Aether.Core.Repositories;
using Aether.Core.Validation;
using Aether.Desktop.Common;

namespace Aether.Desktop.ViewModels;

public sealed class IncidentsViewModel : PageViewModel
{
    public const string AllStatuses = "All statuses";
    public const string AllPlants = "All plants";

    private readonly AppServices _services;
    private string _searchText = "";
    private string _statusFilter = AllStatuses;
    private string _plantFilter = AllPlants;
    private bool _activeOnly;
    private Incident? _selected;
    private IncidentForm? _form;
    private bool _suspendReload;

    public IncidentsViewModel(AppServices services)
    {
        _services = services;
        NewCommand = new RelayCommand(BeginNew, () => !IsEditing);
        EditCommand = new RelayCommand(BeginEdit, () => !IsEditing && Selected is not null);
        DeleteCommand = new RelayCommand(Delete, () => !IsEditing && Selected is not null);
        SaveCommand = new RelayCommand(Save, () => IsEditing);
        CancelCommand = new RelayCommand(Cancel, () => IsEditing);
        ClearFiltersCommand = new RelayCommand(ClearFilters);
    }

    public override string Title => "Incidents";

    public ObservableCollection<Incident> Items { get; } = new();
    public ObservableCollection<string> StatusOptions { get; } = new();
    public ObservableCollection<string> PlantOptions { get; } = new();
    public IReadOnlyList<string> FormStatuses => IncidentStatuses.All;
    public IReadOnlyList<string> PreRiskOptions { get; } = new[] { "", "I", "II", "III", "IV" };
    public FormErrors Errors { get; } = new();

    public RelayCommand NewCommand { get; }
    public RelayCommand EditCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand ClearFiltersCommand { get; }

    public string SearchText { get => _searchText; set { if (Set(ref _searchText, value)) Reload(); } }
    public string StatusFilter { get => _statusFilter; set { if (Set(ref _statusFilter, value ?? AllStatuses)) Reload(); } }
    public string PlantFilter { get => _plantFilter; set { if (Set(ref _plantFilter, value ?? AllPlants)) Reload(); } }
    public bool ActiveOnly { get => _activeOnly; set { if (Set(ref _activeOnly, value)) Reload(); } }

    public Incident? Selected
    {
        get => _selected;
        set
        {
            if (!Set(ref _selected, value)) return;
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(ShowDetails));
            BuildDetails();
        }
    }

    public bool HasSelection => Selected is not null;
    public bool IsEditing => _form is not null;
    public bool IsNotEditing => !IsEditing;
    public bool ShowDetails => !IsEditing && Selected is not null;
    public bool ShowNoSelection => !IsEditing && Selected is null;
    public string FormTitle => _form is null ? "" : _form.Id == 0 ? "New incident" : "Edit incident";
    public IncidentForm? Form { get => _form; private set { Set(ref _form, value); NotifyEditingChanged(); } }

    public IReadOnlyList<DetailRow> Details { get; private set; } = Array.Empty<DetailRow>();
    public string EmptyMessage { get; private set; } = "";
    public bool IsListEmpty => Items.Count == 0;
    public string CountText { get; private set; } = "";

    public override void Refresh()
    {
        ClearMessage();
        var keepId = Selected?.Id;
        IReadOnlyList<string> plants;
        try { plants = _services.Incidents.GetPlants(); }
        catch (AetherException ex) { ShowError(ex.Message); plants = Array.Empty<string>(); }

        _suspendReload = true;
        try
        {
            Sync(StatusOptions, new[] { AllStatuses }.Concat(IncidentStatuses.All));
            Sync(PlantOptions, new[] { AllPlants }.Concat(plants));
            if (!PlantOptions.Contains(_plantFilter)) { _plantFilter = AllPlants; OnPropertyChanged(nameof(PlantFilter)); }
            OnPropertyChanged(nameof(StatusFilter));
            OnPropertyChanged(nameof(PlantFilter));
        }
        finally { _suspendReload = false; }
        Reload(keepId);
    }

    private static void Sync(ObservableCollection<string> target, IEnumerable<string> values)
    {
        var list = values.ToList();
        if (target.SequenceEqual(list)) return;
        target.Clear();
        foreach (var v in list) target.Add(v);
    }

    private IncidentFilter BuildFilter() => new()
    {
        Text = SearchText,
        Status = StatusFilter == AllStatuses ? null : StatusFilter,
        Plant = PlantFilter == AllPlants ? null : PlantFilter,
        ActiveOnly = ActiveOnly,
    };

    private void Reload(int? selectId = null)
    {
        if (_suspendReload) return;
        var keep = selectId ?? Selected?.Id;
        IReadOnlyList<Incident> rows;
        try { rows = _services.Incidents.Search(BuildFilter()); }
        catch (AetherException ex) { ShowError(ex.Message); return; }

        Items.Clear();
        foreach (var r in rows) Items.Add(r);
        Selected = keep is null ? null : Items.FirstOrDefault(i => i.Id == keep);

        var filtered = !string.IsNullOrWhiteSpace(SearchText) || StatusFilter != AllStatuses
                       || PlantFilter != AllPlants || ActiveOnly;
        EmptyMessage = Items.Count > 0 ? ""
            : filtered ? "No incidents match the current search and filters."
            : "No incidents are stored yet. Use New incident to add one.";
        CountText = $"{Items.Count:N0} incident(s) shown";
        OnPropertyChanged(nameof(EmptyMessage));
        OnPropertyChanged(nameof(IsListEmpty));
        OnPropertyChanged(nameof(CountText));
    }

    private void ClearFilters()
    {
        _suspendReload = true;
        SearchText = "";
        StatusFilter = AllStatuses;
        PlantFilter = AllPlants;
        ActiveOnly = false;
        _suspendReload = false;
        Reload();
    }

    private void BuildDetails()
    {
        var i = Selected;
        if (i is null) { Details = Array.Empty<DetailRow>(); OnPropertyChanged(nameof(Details)); return; }

        int actions = 0;
        try { actions = _services.Incidents.CountActions(i.Id); } catch (AetherException) { /* shown elsewhere */ }

        Details = new[]
        {
            DetailRow.Of("Title", i.Title),
            DetailRow.Of("Asset tag", i.TagNumber),
            DetailRow.Of("Plant", i.Plant),
            DetailRow.Of("Status", i.Status),
            DetailRow.Of("Date of occurrence", i.OccurredOn),
            DetailRow.Of("MTO reference", i.Reference),
            DetailRow.Of("AR number", i.ArNo),
            DetailRow.Of("Discipline", i.Discipline),
            DetailRow.Of("Equipment class / type", string.Join(" / ", new[] { i.EquipmentClass, i.EquipmentType }.Where(s => !string.IsNullOrEmpty(s)))),
            DetailRow.Of("Component", i.Component),
            DetailRow.Of("Failure mechanism", i.FailureMechanism),
            DetailRow.Of("Highest impact", i.HighestImpact),
            DetailRow.Of("Pre-Risk class (source)", i.PreRisk),
            DetailRow.Of("Risk score (source)", i.RiskScore),
            DetailRow.Of("RCA PIC", i.PicRca),
            DetailRow.Of("RCA due date", i.RcaDueDate),
            DetailRow.Of("Downtime", i.DowntimeHours, "h"),
            DetailRow.Of("Actual loss", i.ActualLossKUsd, "k USD"),
            DetailRow.Of("Potential loss", i.PotentialLossKUsd, "k USD"),
            DetailRow.Of("Total loss (calculated)", i.TotalLossKUsd, "k USD"),
            DetailRow.Of("Related actions", actions.ToString()),
            DetailRow.Of("Origin", i.Origin == DataOrigin.Source ? "Supplied case data" : "Entered in this application"),
        };
        OnPropertyChanged(nameof(Details));
    }

    private void BeginNew()
    {
        ClearMessage();
        Errors.Clear();
        Form = IncidentForm.ForNew();
    }

    private void BeginEdit()
    {
        if (Selected is null) return;
        ClearMessage();
        Errors.Clear();
        Form = IncidentForm.From(Selected);
    }

    private void Cancel()
    {
        Errors.Clear();
        Form = null;
    }

    private void Save()
    {
        if (_form is null) return;
        var errors = new Dictionary<string, string>();
        var incident = _form.ToIncident(errors);
        foreach (var kv in IncidentValidator.Validate(incident).Errors) errors.TryAdd(kv.Key, kv.Value);

        if (errors.Count > 0)
        {
            Errors.Set(errors);
            ShowError("Please fix the highlighted fields. Nothing was saved.");
            return;
        }

        var isNew = incident.Id == 0;
        var savedId = incident.Id;
        var ok = TryRun(() =>
        {
            if (isNew) savedId = _services.Incidents.Add(incident);
            else _services.Incidents.Update(incident);
        });
        if (!ok) return;

        Errors.Clear();
        Form = null;
        Refresh();
        Selected = Items.FirstOrDefault(i => i.Id == savedId) ?? Selected;
        var what = isNew ? "Incident added" : "Incident changes saved";
        ShowSuccess(Selected is null ? what + ", but it is hidden by the current filters." : what + ".");
    }

    private void Delete()
    {
        if (Selected is null) return;
        var incident = Selected;
        int actions;
        try { actions = _services.Incidents.CountActions(incident.Id); }
        catch (AetherException ex) { ShowError(ex.Message); return; }

        var message = $"Delete incident \"{incident.Title}\" ({incident.TagNumber})?"
            + (actions > 0 ? $"\n\nIts {actions} related action(s) will also be deleted." : "")
            + "\n\nThis cannot be undone.";
        if (!Dialogs.ConfirmDelete("Delete incident", message)) return;

        var removed = 0;
        if (!TryRun(() => removed = _services.Incidents.Delete(incident.Id))) return;

        Selected = null;
        Refresh();
        ShowSuccess(removed > 0
            ? $"Incident deleted together with {removed} related action(s)."
            : "Incident deleted.");
    }

    private void NotifyEditingChanged()
    {
        foreach (var n in new[]
        {
            nameof(IsEditing), nameof(IsNotEditing), nameof(ShowDetails), nameof(ShowNoSelection), nameof(FormTitle),
        })
            OnPropertyChanged(n);
    }
}
