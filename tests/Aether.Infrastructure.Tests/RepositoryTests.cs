using Aether.Core;
using Aether.Core.Models;
using Aether.Core.Repositories;
using Aether.Infrastructure.Repositories;
using Aether.Infrastructure.Seed;
using Xunit;

namespace Aether.Infrastructure.Tests;

public class RepositoryTests
{
    private static Incident NewIncident(string title = "Pump trip", string tag = "PU-1", string plant = "ARP") => new()
    {
        Title = title, Plant = plant, TagNumber = tag, Status = IncidentStatuses.NewRegistered,
        OccurredOn = new DateTime(2026, 5, 1), DowntimeHours = 10, ActualLossKUsd = 200, PotentialLossKUsd = 20,
    };

    private static ActionItem NewAction(int incidentId, string status = ActionStatus.Open) => new()
    {
        IncidentId = incidentId, Description = "Replace seal", Owner = "REL-05",
        TargetDate = new DateTime(2026, 7, 1), Status = status,
    };

    [Fact]
    public void Incident_Crud_RoundTrips()
    {
        using var t = new TestDb();
        var repo = new SqliteIncidentRepository(t.Db);

        var id = repo.Add(NewIncident());
        var created = repo.GetById(id)!;
        Assert.Equal("Pump trip", created.Title);
        Assert.Equal(220, created.TotalLossKUsd);
        Assert.Equal(new DateTime(2026, 5, 1), created.OccurredOn);

        created.Title = "Pump trip (edited)";
        created.DowntimeHours = null;
        created.Status = IncidentStatuses.RiskClosed;
        repo.Update(created);

        var updated = repo.GetById(id)!;
        Assert.Equal("Pump trip (edited)", updated.Title);
        Assert.Null(updated.DowntimeHours);
        Assert.False(updated.IsActive);

        Assert.Equal(0, repo.Delete(id));
        Assert.Null(repo.GetById(id));
    }

    [Fact]
    public void Incident_InvalidData_IsRejectedBeforeSaving()
    {
        using var t = new TestDb();
        var repo = new SqliteIncidentRepository(t.Db);
        var bad = NewIncident(title: " ");
        bad.DowntimeHours = -2;
        var ex = Assert.Throws<ValidationException>(() => repo.Add(bad));
        Assert.Contains(nameof(Incident.Title), ex.Errors.Keys);
        Assert.Contains(nameof(Incident.DowntimeHours), ex.Errors.Keys);
        Assert.Empty(repo.Search());
    }

    [Fact]
    public void Incident_UpdateOrDeleteOfMissingId_ThrowsNotFound()
    {
        using var t = new TestDb();
        var repo = new SqliteIncidentRepository(t.Db);
        var ghost = NewIncident(); ghost.Id = 42;
        Assert.Throws<NotFoundException>(() => repo.Update(ghost));
        Assert.Throws<NotFoundException>(() => repo.Delete(42));
    }

    [Fact]
    public void Incident_SqlText_IsParameterized()
    {
        using var t = new TestDb();
        var repo = new SqliteIncidentRepository(t.Db);
        var id = repo.Add(NewIncident("Robert'); DROP TABLE Incidents;--"));
        Assert.Equal("Robert'); DROP TABLE Incidents;--", repo.GetById(id)!.Title);
        Assert.Single(repo.Search(new IncidentFilter { Text = "'); DROP" }));
        Assert.Single(repo.Search());
    }

    [Fact]
    public void Search_MatchesTextCaseInsensitively_AndReturnsNothingForNoMatch()
    {
        using var t = new TestDb();
        var repo = new SqliteIncidentRepository(t.Db);
        repo.Add(NewIncident("Seal leakage", "PU-2101B"));
        repo.Add(NewIncident("Bearing vibration", "KO-3201", "ZCU"));

        Assert.Single(repo.Search(new IncidentFilter { Text = "seal" }));
        Assert.Single(repo.Search(new IncidentFilter { Text = "ko-3201" }));
        Assert.Empty(repo.Search(new IncidentFilter { Text = "nothing-like-this" }));
        Assert.Empty(repo.Search(new IncidentFilter { Text = "100%" }));      // % is literal, not a wildcard
        Assert.Equal(2, repo.Search(new IncidentFilter { Text = "  " }).Count);
    }

    [Fact]
    public void Search_FiltersByStatusPlantAndActiveOnly_Combined()
    {
        using var t = new TestDb();
        var repo = new SqliteIncidentRepository(t.Db);
        var a = NewIncident("A", plant: "ARP");
        var b = NewIncident("B", plant: "ZCU");
        b.Status = IncidentStatuses.RiskClosed;
        repo.Add(a); repo.Add(b);

        Assert.Single(repo.Search(new IncidentFilter { Plant = "ZCU" }));
        Assert.Single(repo.Search(new IncidentFilter { Status = IncidentStatuses.RiskClosed }));
        Assert.Single(repo.Search(new IncidentFilter { ActiveOnly = true }));
        Assert.Empty(repo.Search(new IncidentFilter { Plant = "ZCU", ActiveOnly = true }));
        Assert.Equal(new[] { "ARP", "ZCU" }, repo.GetPlants());
    }

    [Fact]
    public void Action_Crud_RoundTrips_WithIncidentInfo()
    {
        using var t = new TestDb();
        var incidents = new SqliteIncidentRepository(t.Db);
        var actions = new SqliteActionRepository(t.Db);
        var incidentId = incidents.Add(NewIncident("Seal leakage", "PU-2101B"));

        var id = actions.Add(NewAction(incidentId));
        var loaded = actions.GetById(id)!;
        Assert.Equal("PU-2101B", loaded.IncidentTag);
        Assert.Equal("Seal leakage", loaded.IncidentTitle);
        Assert.Equal(ActionStatus.Open, loaded.Status);

        loaded.Status = ActionStatus.InProgress;
        loaded.Owner = "STA-02";
        actions.Update(loaded);
        Assert.Equal(ActionStatus.InProgress, actions.GetById(id)!.Status);
        Assert.Equal("STA-02", actions.GetById(id)!.Owner);

        actions.Delete(id);
        Assert.Null(actions.GetById(id));
        Assert.Throws<NotFoundException>(() => actions.Delete(id));
    }

    [Fact]
    public void Action_ForUnknownIncident_IsRejected()
    {
        using var t = new TestDb();
        var actions = new SqliteActionRepository(t.Db);
        Assert.Throws<NotFoundException>(() => actions.Add(NewAction(12345)));
    }

    [Fact]
    public void Action_InvalidStatusOrBlankFields_AreRejected()
    {
        using var t = new TestDb();
        var incidentId = new SqliteIncidentRepository(t.Db).Add(NewIncident());
        var actions = new SqliteActionRepository(t.Db);
        var bad = NewAction(incidentId, "Done");
        bad.Owner = "";
        Assert.Throws<ValidationException>(() => actions.Add(bad));
        Assert.Empty(actions.Search());
    }

    [Fact]
    public void Action_Filters_ByStatusIncidentAndText()
    {
        using var t = new TestDb();
        var incidents = new SqliteIncidentRepository(t.Db);
        var actions = new SqliteActionRepository(t.Db);
        var i1 = incidents.Add(NewIncident("One", "T-1"));
        var i2 = incidents.Add(NewIncident("Two", "T-2"));
        actions.Add(NewAction(i1, ActionStatus.Open));
        actions.Add(NewAction(i1, ActionStatus.Closed));
        actions.Add(NewAction(i2, ActionStatus.Open));

        Assert.Equal(2, actions.Search(new ActionFilter { IncidentId = i1 }).Count);
        Assert.Equal(2, actions.Search(new ActionFilter { Status = ActionStatus.Open }).Count);
        Assert.Single(actions.Search(new ActionFilter { IncidentId = i1, Status = ActionStatus.Closed }));
        Assert.Single(actions.Search(new ActionFilter { Text = "T-2" }));
        Assert.Empty(actions.Search(new ActionFilter { Text = "zzz" }));
    }

    [Fact]
    public void DeleteIncident_RemovesItsActionsOnly_InOneStep()
    {
        using var t = new TestDb();
        var incidents = new SqliteIncidentRepository(t.Db);
        var actions = new SqliteActionRepository(t.Db);
        var i1 = incidents.Add(NewIncident("One", "T-1"));
        var i2 = incidents.Add(NewIncident("Two", "T-2"));
        actions.Add(NewAction(i1)); actions.Add(NewAction(i1)); actions.Add(NewAction(i2));

        Assert.Equal(2, incidents.CountActions(i1));
        Assert.Equal(2, incidents.Delete(i1));

        Assert.Null(incidents.GetById(i1));
        Assert.Empty(actions.Search(new ActionFilter { IncidentId = i1 }));
        Assert.Single(actions.Search());   // other incident's action untouched
        Assert.NotNull(incidents.GetById(i2));
    }

    [Fact]
    public void Assets_HaveRecordsWithUnitsDatesAndLimits_AfterSeed()
    {
        using var t = new TestDb();
        SourceDataImporter.EnsureSeeded(t.Db);
        var repo = new SqliteAssetRepository(t.Db);

        var assets = repo.GetAssets();
        Assert.Equal(5, assets.Count);
        Assert.All(assets, a => Assert.Equal(104, a.RecordCount));   // 26 weeks x 4 parameters

        var ko = repo.GetRecordsByTag("KO-3201");
        Assert.Equal(104, ko.Count);
        var vib = ko.Where(r => r.Parameter == "DE Radial Vibration").ToList();
        Assert.Equal(26, vib.Count);
        Assert.All(vib, r => { Assert.Equal("micron", r.Unit); Assert.Equal(45, r.AlarmLimit); Assert.Equal(75, r.TripLimit); });
        Assert.Contains(ko, r => r.HealthStatus == "TRIP");
        Assert.Equal("low", ko.First(r => r.Parameter == "Lube Oil Supply Press").LimitDirection);
    }

    [Fact]
    public void Assets_UnknownTagOrEmptyDatabase_GiveEmptyLists()
    {
        using var t = new TestDb();
        var repo = new SqliteAssetRepository(t.Db);
        Assert.Empty(repo.GetAssets());
        Assert.Empty(repo.GetRecordsByTag("NOPE-1"));
        Assert.Empty(repo.GetRecords(123));
    }

    [Fact]
    public void Dashboard_EmptyDatabase_ReturnsZeros()
    {
        using var t = new TestDb();
        var s = new SqliteDashboardRepository(t.Db).GetStats();
        Assert.True(s.IsEmpty);
        Assert.Equal(0, s.TotalDowntimeHours);
        Assert.Equal(0, s.TotalLossKUsd);
        Assert.Empty(s.RecentIncidents);
        Assert.Empty(s.ByMonth);
    }

    [Fact]
    public void Dashboard_FollowsTheStoredData_AndReportsMissingValues()
    {
        using var t = new TestDb();
        var incidents = new SqliteIncidentRepository(t.Db);
        var dash = new SqliteDashboardRepository(t.Db);

        incidents.Add(NewIncident("A"));                                   // 10 h, 200 + 20
        var open = NewIncident("B"); open.DowntimeHours = 5.5; open.ActualLossKUsd = 100; open.PotentialLossKUsd = null;
        incidents.Add(open);
        var unknown = NewIncident("C"); unknown.DowntimeHours = null; unknown.ActualLossKUsd = null; unknown.PotentialLossKUsd = null;
        unknown.Status = IncidentStatuses.RiskCanceled;
        var cId = incidents.Add(unknown);

        var s = dash.GetStats();
        Assert.Equal(3, s.TotalIncidents);
        Assert.Equal(2, s.ActiveIncidents);
        Assert.Equal(15.5, s.TotalDowntimeHours);
        Assert.Equal(300, s.TotalActualLossKUsd);
        Assert.Equal(20, s.TotalPotentialLossKUsd);
        Assert.Equal(320, s.TotalLossKUsd);
        Assert.Equal(1, s.IncidentsMissingDowntime);
        Assert.Equal(1, s.IncidentsMissingLoss);
        Assert.Equal(3, s.RecentIncidents.Count);
        Assert.Equal("2026-05", Assert.Single(s.ByMonth).Month);

        incidents.Delete(cId);                                              // statistics follow the change
        var after = dash.GetStats();
        Assert.Equal(2, after.TotalIncidents);
        Assert.Equal(0, after.IncidentsMissingDowntime);
    }

    [Fact]
    public void Dashboard_Distributions_AreComputed()
    {
        using var t = new TestDb();
        var incidents = new SqliteIncidentRepository(t.Db);
        var actions = new SqliteActionRepository(t.Db);
        var id = incidents.Add(NewIncident("A", plant: "ZCU"));
        incidents.Add(NewIncident("B", plant: "ZCU"));
        incidents.Add(NewIncident("C", plant: "ARP"));
        actions.Add(NewAction(id, ActionStatus.Closed));
        actions.Add(NewAction(id, ActionStatus.Open));

        var s = new SqliteDashboardRepository(t.Db).GetStats();
        Assert.Equal(new LabelCount("ZCU", 2), s.ByPlant[0]);
        Assert.Equal(3, s.ByStatus.Single(x => x.Label == IncidentStatuses.NewRegistered).Count);
        Assert.Equal(new[] { ActionStatus.Open, ActionStatus.Closed }, s.ActionsByStatus.Select(x => x.Label));
    }

    [Fact]
    public void Dashboard_RecentIncidents_AreNewestFirst_AndLimited()
    {
        using var t = new TestDb();
        var incidents = new SqliteIncidentRepository(t.Db);
        for (var d = 1; d <= 12; d++)
        {
            var i = NewIncident("I" + d); i.OccurredOn = new DateTime(2026, 1, d);
            incidents.Add(i);
        }
        var recent = new SqliteDashboardRepository(t.Db).GetStats(5).RecentIncidents;
        Assert.Equal(5, recent.Count);
        Assert.Equal("I12", recent[0].Title);
    }

    [Fact]
    public void Priority_OfSeededHeroIncident_UsesStoredConditionRecords()
    {
        using var t = new TestDb();
        SourceDataImporter.EnsureSeeded(t.Db);
        var incidents = new SqliteIncidentRepository(t.Db);
        var assets = new SqliteAssetRepository(t.Db);

        var ko = incidents.Search(new IncidentFilter { Text = "KO-3201" }).Single();
        var result = Aether.Core.Services.PriorityService.Evaluate(ko, assets.GetRecordsByTag(ko.TagNumber));

        Assert.Equal(PriorityCategory.Critical, result.Category);
        Assert.Equal(8, result.Points);                                  // 3 downtime + 3 loss + 2 condition
        Assert.Equal(FactorState.Scored, result.Factors[2].State);
        Assert.Equal("II", result.SourcePreRisk);

        // An incident whose tag has no asset record gets no condition score, and that is explained.
        var other = incidents.Search().First(i => i.TagNumber != "KO-3201" && assets.GetRecordsByTag(i.TagNumber).Count == 0);
        var plain = Aether.Core.Services.PriorityService.Evaluate(other, assets.GetRecordsByTag(other.TagNumber));
        Assert.Equal(FactorState.NotAvailable, plain.Factors[2].State);
    }
}
