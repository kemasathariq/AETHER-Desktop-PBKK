using System.Text;
using Aether.Core.Repositories;
using Aether.Infrastructure.Data;
using Aether.Infrastructure.Repositories;
using Aether.Infrastructure.Seed;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Aether.Infrastructure.Tests;

public class DatabaseAndSeedTests
{
    [Fact]
    public void Initialize_CreatesFolderFileAndTables_AndIsRepeatable()
    {
        using var t = new TestDb(initialize: false);
        t.Db.Initialize();
        t.Db.Initialize();
        Assert.True(File.Exists(t.Path_));

        using var c = t.Db.OpenConnection();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;";
        var tables = new List<string>();
        using (var r = cmd.ExecuteReader()) while (r.Read()) tables.Add(r.GetString(0));
        foreach (var expected in new[] { "ActionItems", "Assets", "ConditionRecords", "Incidents", "SeedLog" })
            Assert.Contains(expected, tables);
    }

    [Fact]
    public void ForeignKeys_AreEnforced()
    {
        using var t = new TestDb();
        using var c = t.Db.OpenConnection();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "INSERT INTO ActionItems (IncidentId, Description, Owner, TargetDate, Status) VALUES (999,'x','y','2026-01-01','Open');";
        Assert.Throws<SqliteException>(() => cmd.ExecuteNonQuery());
    }

    [Fact]
    public void Seed_LoadsAllCaseData()
    {
        using var t = new TestDb();
        var report = SourceDataImporter.EnsureSeeded(t.Db);
        Assert.True(report.Applied);
        Assert.Equal(380, report.Incidents);
        Assert.Equal(5, report.Assets);
        Assert.Equal(520, report.ConditionRecords);
        Assert.Equal(20, report.Actions);
        Assert.Empty(report.Warnings);
    }

    [Fact]
    public void Seed_TotalsMatchTheSource()
    {
        using var t = new TestDb();
        SourceDataImporter.EnsureSeeded(t.Db);
        var s = new SqliteDashboardRepository(t.Db).GetStats();
        Assert.Equal(380, s.TotalIncidents);
        Assert.Equal(2261.1, s.TotalDowntimeHours, 1);
        Assert.Equal(67194.43, s.TotalLossKUsd, 1);
    }

    [Fact]
    public void Seed_RunTwice_DoesNotDuplicate()
    {
        using var t = new TestDb();
        SourceDataImporter.EnsureSeeded(t.Db);
        var second = SourceDataImporter.EnsureSeeded(t.Db);
        Assert.False(second.Applied);
        Assert.Equal(380, new SqliteIncidentRepository(t.Db).Search().Count);
        Assert.Equal(20, new SqliteActionRepository(t.Db).Search().Count);
    }

    [Fact]
    public void Seed_DoesNotReturnDeletedRows_AfterRestart()
    {
        using var t = new TestDb();
        SourceDataImporter.EnsureSeeded(t.Db);
        var repo = new SqliteIncidentRepository(t.Db);
        repo.Delete(repo.Search().First().Id);

        var restarted = new AetherDatabase(t.Path_);
        restarted.Initialize();
        SourceDataImporter.EnsureSeeded(restarted);
        Assert.Equal(379, new SqliteIncidentRepository(restarted).Search().Count);
    }

    [Fact]
    public void Seed_DuplicateMtoNumber_KeepsBothIncidents()
    {
        using var t = new TestDb();
        SourceDataImporter.EnsureSeeded(t.Db);
        var found = new SqliteIncidentRepository(t.Db).Search(new IncidentFilter { Text = "MTO-2026-OPP-0096" });
        Assert.Equal(2, found.Count);
    }

    [Fact]
    public void Seed_ActionsAreLinkedToTheirIncident()
    {
        using var t = new TestDb();
        SourceDataImporter.EnsureSeeded(t.Db);
        var actions = new SqliteActionRepository(t.Db).Search(new ActionFilter { Text = "KO-3201" });
        Assert.Equal(4, actions.Count);
        Assert.All(actions, a => Assert.Equal("KO-3201", a.IncidentTag));
        Assert.Equal(1, actions.Count(a => a.Status == "Closed"));
        Assert.Equal(1, actions.Count(a => a.Status == "In Progress"));
        Assert.Equal(2, actions.Count(a => a.Status == "Open"));
    }

    [Fact]
    public void Seed_MalformedValues_AreSkippedOrNulledWithWarnings()
    {
        const string json = """
        {
          "incidents": [
            { "sourceSerialNo": 1, "title": "Good", "plant": "P", "tagNumber": "T-1", "status": "RISK CLOSED",
              "occurredOn": "not-a-date", "downtimeHours": -5, "actualLossKUsd": 10.5 },
            { "sourceSerialNo": 2, "title": "", "plant": "P", "tagNumber": "T-2", "status": "RISK CLOSED" },
            { "sourceSerialNo": 3, "title": "Bad status", "plant": "P", "tagNumber": "T-3", "status": "WEIRD" }
          ],
          "assets": [], "conditionRecords": [],
          "actions": [ { "incidentSerialNo": 99, "description": "x", "owner": "o", "targetDate": "2026-01-01", "status": "Open" } ]
        }
        """;
        using var t = new TestDb();
        var report = SourceDataImporter.EnsureSeeded(t.Db, new MemoryStream(Encoding.UTF8.GetBytes(json)));
        Assert.Equal(1, report.Incidents);
        Assert.Equal(0, report.Actions);
        Assert.True(report.Warnings.Count >= 4);

        var inc = Assert.Single(new SqliteIncidentRepository(t.Db).Search());
        Assert.Null(inc.OccurredOn);
        Assert.Null(inc.DowntimeHours);
        Assert.Equal(10.5, inc.ActualLossKUsd);
    }

    [Fact]
    public void Data_PersistsAcrossRestart()
    {
        using var t = new TestDb();
        SourceDataImporter.EnsureSeeded(t.Db);
        var id = new SqliteIncidentRepository(t.Db).Add(new Aether.Core.Models.Incident
        {
            Title = "Persist me", Plant = "ZCU", TagNumber = "KO-3201",
            OccurredOn = new DateTime(2026, 8, 1), Status = "NEW REGISTERED", DowntimeHours = 3.5,
        });

        var restarted = new AetherDatabase(t.Path_);
        restarted.Initialize();
        var loaded = new SqliteIncidentRepository(restarted).GetById(id);
        Assert.NotNull(loaded);
        Assert.Equal("Persist me", loaded!.Title);
        Assert.Equal(3.5, loaded.DowntimeHours);
        Assert.Equal("User", loaded.Origin);
    }
}
