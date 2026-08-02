using System.Collections.Concurrent;
using System.Text.Json;
using MongoDB.Bson;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Domain.Scenarios;

public abstract class BaseScenario : IMockScenario
{
    private readonly ConcurrentDictionary<string, object> collections = new(StringComparer.OrdinalIgnoreCase);
    private readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new Models.Converters.CustomFormQuestionJsonConverter(),
            new Models.Converters.CustomFormAnswerJsonConverter()
        }
    };

    protected BaseScenario()
    {
        Reset();
    }

    public void Reset()
    {
        collections.Clear();
        Initialize();
        MockScenarioValidator.Validate(this);
    }

    protected abstract void Initialize();

    protected void Set<T>(string collectionName, IEnumerable<T> items) where T : BaseEntity =>
        collections[collectionName] = items.Select(Clone).ToList();

    public IList<T> Read<T>(string collectionName) where T : BaseEntity
    {
        var collection = GetCollection<T>(collectionName);
        lock (collection)
        {
            return Clone(collection.OrderByDescending(item => item.CreationDateTime).ToList());
        }
    }

    public T Add<T>(string collectionName, T item) where T : BaseEntity
    {
        var collection = GetCollection<T>(collectionName);
        var storedItem = item;

        if (string.IsNullOrWhiteSpace(storedItem.Id))
            storedItem = (T)(storedItem with { Id = ObjectId.GenerateNewId().ToString() });

        if (storedItem.CreationDateTime == default)
            storedItem = (T)(storedItem with { CreationDateTime = DateTime.UtcNow });

        lock (collection)
        {
            collection.Add(Clone(storedItem));
        }

        return Clone(storedItem);
    }

    public void Replace<T>(string collectionName, T item) where T : BaseEntity
    {
        if (string.IsNullOrWhiteSpace(item.Id))
            return;

        var collection = GetCollection<T>(collectionName);
        lock (collection)
        {
            var index = collection.FindIndex(existing => existing.Id == item.Id);
            if (index >= 0)
                collection[index] = Clone(item);
        }
    }

    public void Delete<T>(string collectionName, string id) where T : BaseEntity
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        var collection = GetCollection<T>(collectionName);
        lock (collection)
        {
            collection.RemoveAll(item => item.Id == id);
        }
    }

    private List<T> GetCollection<T>(string collectionName) where T : BaseEntity
    {
        if (!collections.TryGetValue(collectionName, out var collection))
            throw new InvalidOperationException($"Scenario collection '{collectionName}' is not initialized.");

        if (collection is not List<T> typedCollection)
            throw new InvalidOperationException($"Scenario collection '{collectionName}' is not a collection of {typeof(T).Name}.");

        return typedCollection;
    }

    private T Clone<T>(T item) where T : BaseEntity =>
        JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(item, jsonOptions), jsonOptions)!;

    private List<T> Clone<T>(List<T> items) where T : BaseEntity =>
        JsonSerializer.Deserialize<List<T>>(JsonSerializer.Serialize(items, jsonOptions), jsonOptions) ?? [];
}
