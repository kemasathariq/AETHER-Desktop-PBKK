using System.Collections.ObjectModel;
using Aether.Core;
using Aether.Core.Models;
using Aether.Core.Repositories;
using Aether.Core.Validation;
using Aether.Desktop.Common;

namespace Aether.Desktop.ViewModels;

public sealed record IncidentOption(int Id, string Display);

/// <summary>Editable copy of an action.</summary>
public sealed class ActionForm : ObservableObject
{
    private int _incidentId;
    private string _description = "", _owner = "", _status = ActionStatus.Open;
    private DateTime? _targetDate;

    public int Id { get; init; }
    public string? Category { get; init; }
    public string? RootCauseCode { get; init; }
    public string Origin { get; init; } = DataOrigin.User;

    public int IncidentId { get => _incidentId; set => Set(ref _incidentId, value); }
    public string Description { get => _description; set => Set(ref _description, value); }
    public string Owner { get => _owner; set => Set(ref _owner, value); }
    public DateTime? TargetDate { get => _targetDate; set => Set(ref _targetDate, value); }
    public string Status { get => _status; set => Set(ref _status, value); }

    public ActionItem ToAction() => new()
    {
        Id = Id, IncidentId = IncidentId, Description = Description, Owner = Owner, TargetDate = TargetDate,
        Status = Status, Category = Category, RootCauseCode = RootCauseCode, Origin = Origin,
    };
}

public sealed class ActionsViewModel : PageViewModel
{
    public const string AllStatuses = "All statuses";

    private readonly AppServices _services;
    private string _searchText = "";
    private string _statusFilter = AllStatuses;
    private ActionItem? _selected;
    private ActionForm? _form;
    private bool _suspendReload;

    public ActionsViewModel(AppServices services)
    {
        _services = services;
        NewCommand = new RelayCommand(BeginNew, () => !IsEditing);
        EditCommand = new RelayCommand(BeginEdit, () => !IsEditing && Selected is not null);
        DeleteCommand = new RelayCommand(Delete, () => !IsEditing && Selected is not null);
        SaveCommand = new RelayCommand(Save, () => IsEditing);
        CancelCommand = new RelayCommand(Cancel, () => IsEditing);
        ClearFiltersCommand = new RelayCommand(ClearFilters);
    }

    public override string Title => "Action Tracker";

    public ObservableCollection<ActionItem> Items { get; } = new();
    public ObservableCollection<IncidentOption> IncidentOptions { get; } = new();
    public IReadOnlyList<string> FilterStatuses { get; } = new[] { AllStatuses }.Concat(ActionStatus.All).ToList();
    public IReadOnlyList<string> FormStatuses => ActionStatus.All;
    public FormErrors Errors { get; } = new();

    public RelayCommand NewCommand { get; }
    public RelayCommand EditCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand ClearFiltersCommand { get; }

    public string SearchText { get => _searchText; set { if (Set(ref _searchText, value)) Reload(); } }
    public string StatusFilter { get => _statusFilter; set { if (Set(ref _statusFilter, value ?? AllStatuses)) Reload(); } }

    public ActionItem? Selected
    {
        get => _selected;
        set
        {
            if (!Set(ref _selected, value)) return;
            OnPropertyChanged(nameof(ShowDetails));
            OnPropertyChanged(nameof(ShowNoSelection));
            BuildDetails();
        }
    }

    public bool IsEditing => _form is not null;
    public bool IsNotEditing => !IsEditing;
    public bool ShowDetails => !IsEditing && Selected is not null;
    public bool ShowNoSelection => !IsEditing && Selected is null;
    public string FormTitle => _form is null ? "" : _form.Id == 0 ? "New action" : "Edit action";
    public ActionForm? Form { get => _form; private set { Set(ref _form, value); NotifyEditingChanged(); } }

    public IReadOnlyList<DetailRow> Details { get; private set; } = Array.Empty<DetailRow>();
    public string EmptyMessage { get; private set; } = "";
    public bool IsListEmpty => Items.Count == 0;
    public string CountText { get; private set; } = "";

    public override void Refresh()
    {
        ClearMessage();
        var keepId = Selected?.Id;
        try
        {
            var options = _services.Incidents.Search()
                .Select(i => new IncidentOption(i.Id, $"{i.TagNumber} · {i.Title} ({i.OccurredOn:yyyy-MM-dd})"))
                .ToList();
            IncidentOptions.Clear();
            foreach (var o in options) IncidentOptions.Add(o);
        }
        catch (AetherException ex) { ShowError(ex.Message); }
        Reload(keepId);
    }

    private void Reload(int? selectId = null)
    {
        if (_suspendReload) return;
        var keep = selectId ?? Selected?.Id;
        IReadOnlyList<ActionItem> rows;
        try
        {
            rows = _services.Actions.Search(new ActionFilter
            {
                Text = SearchText,
                Status = StatusFilter == AllStatuses ? null : StatusFilter,
            });
        }
        catch (AetherException ex) { ShowError(ex.Message); return; }

        Items.Clear();
        foreach (var r in rows) Items.Add(r);
        Selected = keep is null ? null : Items.FirstOrDefault(a => a.Id == keep);

        var filtered = !string.IsNullOrWhiteSpace(SearchText) || StatusFilter != AllStatuses;
        EmptyMessage = Items.Count > 0 ? ""
            : filtered ? "No actions match the current search and filter."
            : "No actions are stored yet. Use New action to add one for an incident.";
        CountText = $"{Items.Count:N0} action(s) shown";
        OnPropertyChanged(nameof(EmptyMessage));
        OnPropertyChanged(nameof(IsListEmpty));
        OnPropertyChanged(nameof(CountText));
    }

    private void ClearFilters()
    {
        _suspendReload = true;
        SearchText = "";
        StatusFilter = AllStatuses;
        _suspendReload = false;
        Reload();
    }

    private void BuildDetails()
    {
        var a = Selected;
        if (a is null) { Details = Array.Empty<DetailRow>(); OnPropertyChanged(nameof(Details)); return; }
        Details = new[]
        {
            DetailRow.Of("Description", a.Description),
            DetailRow.Of("Related incident", $"{a.IncidentTag} · {a.IncidentTitle}"),
            DetailRow.Of("Owner", a.Owner),
            DetailRow.Of("Target date", a.TargetDate),
            DetailRow.Of("Status", a.Status),
            DetailRow.Of("Type", a.Category),
            DetailRow.Of("Root cause code", a.RootCauseCode),
            DetailRow.Of("Origin", a.Origin == DataOrigin.Source ? "Supplied RCA data" : "Entered in this application"),
        };
        OnPropertyChanged(nameof(Details));
    }

    private void BeginNew()
    {
        ClearMessage();
        Errors.Clear();
        if (IncidentOptions.Count == 0)
        {
            ShowError("There are no incidents yet. Add an incident first, then add actions for it.");
            return;
        }
        Form = new ActionForm { TargetDate = DateTime.Today.AddDays(14) };
    }

    private void BeginEdit()
    {
        if (Selected is null) return;
        ClearMessage();
        Errors.Clear();
        var a = Selected;
        Form = new ActionForm
        {
            Id = a.Id, IncidentId = a.IncidentId, Description = a.Description, Owner = a.Owner,
            TargetDate = a.TargetDate, Status = a.Status, Category = a.Category,
            RootCauseCode = a.RootCauseCode, Origin = a.Origin,
        };
    }

    private void Cancel()
    {
        Errors.Clear();
        Form = null;
    }

    private void Save()
    {
        if (_form is null) return;
        var action = _form.ToAction();
        var result = ActionValidator.Validate(action);
        if (!result.IsValid)
        {
            Errors.Set(result.Errors);
            ShowError("Please fix the highlighted fields. Nothing was saved.");
            return;
        }

        var isNew = action.Id == 0;
        var savedId = action.Id;
        if (!TryRun(() =>
            {
                if (isNew) savedId = _services.Actions.Add(action);
                else _services.Actions.Update(action);
            })) return;

        Errors.Clear();
        Form = null;
        Reload(savedId);
        ShowSuccess(isNew ? "Action added." : "Action changes saved.");
    }

    private void Delete()
    {
        if (Selected is null) return;
        var a = Selected;
        if (!Dialogs.ConfirmDelete("Delete action",
                $"Delete the action \"{a.Description}\" for {a.IncidentTag}?\n\nThis cannot be undone.")) return;
        if (!TryRun(() => _services.Actions.Delete(a.Id))) return;

        Selected = null;
        Reload();
        ShowSuccess("Action deleted.");
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
