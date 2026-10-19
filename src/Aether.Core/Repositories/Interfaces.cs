using Aether.Core.Models;

namespace Aether.Core.Repositories;

/// <remarks>
/// Conventions for all repositories: methods validate their input and throw
/// <see cref="ValidationException"/>; missing rows raise <see cref="NotFoundException"/>;
/// database failures are wrapped in <see cref="AetherException"/> with a readable message.
/// </remarks>
public interface IIncidentRepository
{
    IReadOnlyList<Incident> Search(IncidentFilter? filter = null);
    Incident? GetById(int id);

    /// <summary>Inserts the incident and returns its new ID.</summary>
    int Add(Incident incident);
    void Update(Incident incident);

    /// <summary>Number of actions that Delete would remove together with the incident.</summary>
    int CountActions(int incidentId);

    /// <summary>Deletes the incident and its actions in one transaction. Returns the number of actions removed.</summary>
    int Delete(int incidentId);

    IReadOnlyList<string> GetPlants();
    IReadOnlyList<string> GetDisciplines();
}

public interface IActionRepository
{
    IReadOnlyList<ActionItem> Search(ActionFilter? filter = null);
    ActionItem? GetById(int id);
    int Add(ActionItem action);
    void Update(ActionItem action);
    void Delete(int actionId);
}

public interface IAssetRepository
{
    /// <summary>All assets with <see cref="Asset.RecordCount"/> filled in.</summary>
    IReadOnlyList<Asset> GetAssets();

    /// <summary>Records of one asset ordered by date, then parameter. Empty when none are stored.</summary>
    IReadOnlyList<ConditionRecord> GetRecords(int assetId);

    /// <summary>Records for an equipment tag, or empty when the tag has no asset.</summary>
    IReadOnlyList<ConditionRecord> GetRecordsByTag(string tag);
}

public interface IDashboardRepository
{
    DashboardStats GetStats(int recentCount = 8);
}
