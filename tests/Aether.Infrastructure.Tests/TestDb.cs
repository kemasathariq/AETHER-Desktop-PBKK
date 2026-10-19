using Aether.Infrastructure.Data;

namespace Aether.Infrastructure.Tests;

/// <summary>A temporary SQLite file that is deleted when the test ends.</summary>
public sealed class TestDb : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "aether-tests-" + Guid.NewGuid().ToString("N"));

    public AetherDatabase Db { get; }
    public string Path_ => Db.DatabasePath;

    public TestDb(bool initialize = true)
    {
        Db = new AetherDatabase(Path.Combine(_folder, "nested", "test.db"));
        if (initialize) Db.Initialize();
    }

    public void Dispose()
    {
        try { Directory.Delete(_folder, true); } catch (IOException) { /* temp folder, best effort */ }
    }
}
