using Aether.Core.Repositories;
using Aether.Infrastructure.Data;
using Aether.Infrastructure.Repositories;
using Aether.Infrastructure.Seed;

namespace Aether.Desktop.Common;

/// <summary>Holds the repositories used by the view models. Created once at startup (no DI framework).</summary>
public sealed class AppServices
{
    public required string DatabasePath { get; init; }
    public required SeedReport SeedReport { get; init; }
    public required IIncidentRepository Incidents { get; init; }
    public required IActionRepository Actions { get; init; }
    public required IAssetRepository Assets { get; init; }
    public required IDashboardRepository Dashboard { get; init; }

    /// <summary>
    /// Creates the database in the user's local app data folder, then imports the case data once.
    /// The AETHER_DB_PATH environment variable overrides the location (used for testing and demos).
    /// </summary>
    public static AppServices Create()
    {
        var path = Environment.GetEnvironmentVariable("AETHER_DB_PATH");
        var db = new AetherDatabase(string.IsNullOrWhiteSpace(path) ? AetherDatabase.DefaultPath() : path);
        db.Initialize();
        var report = SourceDataImporter.EnsureSeeded(db);

        return new AppServices
        {
            DatabasePath = db.DatabasePath,
            SeedReport = report,
            Incidents = new SqliteIncidentRepository(db),
            Actions = new SqliteActionRepository(db),
            Assets = new SqliteAssetRepository(db),
            Dashboard = new SqliteDashboardRepository(db),
        };
    }
}
