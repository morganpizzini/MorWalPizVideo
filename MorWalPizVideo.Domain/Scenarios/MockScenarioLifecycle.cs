using Microsoft.Extensions.Configuration;

namespace MorWalPizVideo.Domain.Scenarios;

public sealed class MockScenarioLifecycle : IMockScenarioLifecycle
{
    private readonly object sync = new();
    private IMockScenario current;
    private string name;

    public MockScenarioLifecycle(IConfiguration configuration)
    {
        name = MockScenarioNames.Normalize(configuration["MockScenario"] ?? configuration["FeatureManagement:MockScenario"]);
        current = Create(name);
    }

    public string Name
    {
        get { lock (sync) return name; }
    }

    public void Select(string scenarioName)
    {
        var normalizedName = MockScenarioNames.Normalize(scenarioName);
        lock (sync)
        {
            name = normalizedName;
            current = Create(normalizedName);
        }
    }

    public void Reinitialize()
    {
        lock (sync)
            current = Create(name);
    }

    public void Reset()
    {
        lock (sync)
            current.Reset();
    }

    public IList<T> Read<T>(string collectionName) where T : Server.Models.BaseEntity
    {
        lock (sync) return current.Read<T>(collectionName);
    }

    public T Add<T>(string collectionName, T item) where T : Server.Models.BaseEntity
    {
        lock (sync) return current.Add(collectionName, item);
    }

    public void Replace<T>(string collectionName, T item) where T : Server.Models.BaseEntity
    {
        lock (sync) current.Replace(collectionName, item);
    }

    public void Delete<T>(string collectionName, string id) where T : Server.Models.BaseEntity
    {
        lock (sync) current.Delete<T>(collectionName, id);
    }

    private static IMockScenario Create(string scenarioName) => scenarioName switch
    {
        MockScenarioNames.Empty => new EmptyScenario(),
        MockScenarioNames.Authorization => new AuthorizationScenario(),
        MockScenarioNames.ExternalFailure => new ExternalFailureScenario(),
        MockScenarioNames.LegacyCompatibility => new LegacyCompatibilityScenario(),
        _ => new PrimaryScenario()
    };
}