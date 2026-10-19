using Aether.Core.Models;
using Aether.Core.Validation;
using Aether.Desktop.Common;

namespace Aether.Desktop.ViewModels;

/// <summary>Editable text copy of an incident. Numbers are kept as text so bad input can be reported.</summary>
public sealed class IncidentForm : ObservableObject
{
    private string _title = "", _reference = "", _arNo = "", _plant = "", _tag = "", _eqClass = "", _eqType = "";
    private string _discipline = "", _component = "", _mechanism = "", _impact = "", _preRisk = "", _pic = "";
    private string _status = IncidentStatuses.NewRegistered;
    private string _downtime = "", _actual = "", _potential = "", _riskScore = "";
    private DateTime? _occurredOn, _rcaDue;

    public int Id { get; private set; }
    public int? SourceSerialNo { get; private set; }
    public string Origin { get; private set; } = DataOrigin.User;

    public string Title { get => _title; set => Set(ref _title, value); }
    public string Reference { get => _reference; set => Set(ref _reference, value); }
    public string ArNo { get => _arNo; set => Set(ref _arNo, value); }
    public string Plant { get => _plant; set => Set(ref _plant, value); }
    public string TagNumber { get => _tag; set => Set(ref _tag, value); }
    public string EquipmentClass { get => _eqClass; set => Set(ref _eqClass, value); }
    public string EquipmentType { get => _eqType; set => Set(ref _eqType, value); }
    public string Discipline { get => _discipline; set => Set(ref _discipline, value); }
    public string Component { get => _component; set => Set(ref _component, value); }
    public string FailureMechanism { get => _mechanism; set => Set(ref _mechanism, value); }
    public string HighestImpact { get => _impact; set => Set(ref _impact, value); }
    public string PreRisk { get => _preRisk; set => Set(ref _preRisk, value); }
    public string PicRca { get => _pic; set => Set(ref _pic, value); }
    public string Status { get => _status; set => Set(ref _status, value); }
    public DateTime? OccurredOn { get => _occurredOn; set => Set(ref _occurredOn, value); }
    public DateTime? RcaDueDate { get => _rcaDue; set => Set(ref _rcaDue, value); }
    public string DowntimeText { get => _downtime; set => Set(ref _downtime, value); }
    public string ActualLossText { get => _actual; set => Set(ref _actual, value); }
    public string PotentialLossText { get => _potential; set => Set(ref _potential, value); }
    public string RiskScoreText { get => _riskScore; set => Set(ref _riskScore, value); }

    public static IncidentForm ForNew() => new() { OccurredOn = DateTime.Today };

    public static IncidentForm From(Incident i) => new()
    {
        Id = i.Id, SourceSerialNo = i.SourceSerialNo, Origin = i.Origin,
        Title = i.Title, Reference = i.Reference ?? "", ArNo = i.ArNo ?? "", Plant = i.Plant, TagNumber = i.TagNumber,
        EquipmentClass = i.EquipmentClass ?? "", EquipmentType = i.EquipmentType ?? "", Discipline = i.Discipline ?? "",
        Component = i.Component ?? "", FailureMechanism = i.FailureMechanism ?? "", HighestImpact = i.HighestImpact ?? "",
        PreRisk = i.PreRisk ?? "", PicRca = i.PicRca ?? "", Status = i.Status,
        OccurredOn = i.OccurredOn, RcaDueDate = i.RcaDueDate,
        DowntimeText = NumberParser.Format(i.DowntimeHours), ActualLossText = NumberParser.Format(i.ActualLossKUsd),
        PotentialLossText = NumberParser.Format(i.PotentialLossKUsd), RiskScoreText = NumberParser.Format(i.RiskScore),
    };

    /// <summary>
    /// Converts the form into an incident. Text that is not a number is reported in <paramref name="errors"/>
    /// (the rest of the validation is done by <see cref="IncidentValidator"/>).
    /// </summary>
    public Incident ToIncident(IDictionary<string, string> errors)
    {
        double? Parse(string text, string field, string label)
        {
            if (NumberParser.TryParseOptional(text, out var v)) return v;
            errors[field] = $"{label} must be a number, for example 12.5.";
            return null;
        }

        return new Incident
        {
            Id = Id, SourceSerialNo = SourceSerialNo, Origin = Origin,
            Title = Title, Reference = Reference, ArNo = ArNo, Plant = Plant, TagNumber = TagNumber,
            EquipmentClass = EquipmentClass, EquipmentType = EquipmentType, Discipline = Discipline,
            Component = Component, FailureMechanism = FailureMechanism, HighestImpact = HighestImpact,
            PreRisk = PreRisk, PicRca = PicRca, Status = Status, OccurredOn = OccurredOn, RcaDueDate = RcaDueDate,
            DowntimeHours = Parse(DowntimeText, nameof(Incident.DowntimeHours), "Downtime"),
            ActualLossKUsd = Parse(ActualLossText, nameof(Incident.ActualLossKUsd), "Actual loss"),
            PotentialLossKUsd = Parse(PotentialLossText, nameof(Incident.PotentialLossKUsd), "Potential loss"),
            RiskScore = Parse(RiskScoreText, nameof(Incident.RiskScore), "Risk score"),
        };
    }
}
