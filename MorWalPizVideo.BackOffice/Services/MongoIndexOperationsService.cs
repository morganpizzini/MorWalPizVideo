using MongoDB.Bson;
using MongoDB.Driver;
using MorWalPizVideo.Models.Constraints;

namespace MorWalPizVideo.BackOffice.Services;

public sealed record MongoIndexManifestEntry(
    string Key,
    string Collection,
    string Name,
    BsonDocument Keys,
    bool Unique = false,
    BsonDocument? PartialFilter = null);

public sealed record MongoIndexAuditItem(
    string Key,
    string Collection,
    string Name,
    bool Exists,
    string SpecJson);

public sealed record MongoIndexApplyResult(
    string Key,
    string Collection,
    string Name,
    string Action);

public sealed record MongoIndexRemovalEntry(
    string Key,
    string Collection,
    string Name,
    string ReplacementKey);

public sealed record MongoIndexRemovalResult(
    string Key,
    string Collection,
    string Name,
    string Action);

public sealed class MongoIndexOperationValidationException(string message) : Exception(message);

public sealed class MongoIndexOperationException(string message, Exception innerException)
    : Exception(message, innerException);

public interface IMongoIndexOperationsService
{
    Task<IList<MongoIndexAuditItem>> AuditAsync(IList<string>? keys = null, CancellationToken cancellationToken = default);
    Task<IList<MongoIndexApplyResult>> ApplyAsync(IList<string> approvedKeys, CancellationToken cancellationToken = default);
    Task<IList<MongoIndexRemovalResult>> RemoveAsync(IList<string> approvedRemovalKeys, CancellationToken cancellationToken = default);
}

public sealed class MongoIndexOperationsService(IMongoDatabase database) : IMongoIndexOperationsService
{
    internal static readonly IReadOnlyList<MongoIndexManifestEntry> Manifest =
    [
        new(
            Key: "shortlinks.code.unique",
            Collection: DbCollections.ShortLinks,
            Name: "ux_shortlinks_code_ci",
            Keys: new BsonDocument("code", 1),
            Unique: true),
        new(
            Key: "customformresponses.formid_submittedat_desc",
            Collection: DbCollections.CustomFormResponses,
            Name: "ix_customformresponses_formid_submittedat_desc",
            Keys: new BsonDocument { { "formId", 1 }, { "submittedAt", -1 } }),
        new(
            Key: "customformresponses.formid_responseid.unique",
            Collection: DbCollections.CustomFormResponses,
            Name: "ux_customformresponses_formid_responseid",
            Keys: new BsonDocument { { "formId", 1 }, { "responseId", 1 } },
            Unique: true),
        new(
            Key: "youtubecontent_isprivate_creation_desc",
            Collection: DbCollections.YouTubeContent,
            Name: "ix_youtubecontent_isprivate_creation_desc",
            Keys: new BsonDocument { { "isPrivate", 1 }, { "creationDateTime", -1 } }),
        new(
            Key: "youtubecontent_isprivate_latestpublished_creation_desc",
            Collection: DbCollections.YouTubeContent,
            Name: "ix_youtubecontent_isprivate_latestpublished_creation_desc",
            Keys: new BsonDocument
            {
                { "isPrivate", 1 },
                { "latestPublishedAt", -1 },
                { "creationDateTime", -1 }
            }),
        new(
            Key: "pages_url.unique",
            Collection: DbCollections.Pages,
            Name: "ux_pages_url_ci",
            Keys: new BsonDocument("url", 1),
            Unique: true),
        new(
            Key: "navigation_channel.unique",
            Collection: DbCollections.ChannelNavigations,
            Name: "ux_navigation_channel",
            Keys: new BsonDocument("channelId", 1),
            Unique: true),
        new(
            Key: "compilations_url.unique",
            Collection: DbCollections.Compilations,
            Name: "ux_compilations_url_ci",
            Keys: new BsonDocument("url", 1),
            Unique: true),
        new(
            Key: "quicklinks_url.unique",
            Collection: DbCollections.QuickLinks,
            Name: "ux_quicklinks_url_ci",
            Keys: new BsonDocument("url", 1),
            Unique: true),
        new(
            Key: "customforms_active_url",
            Collection: DbCollections.CustomForms,
            Name: "ix_customforms_active_url",
            Keys: new BsonDocument { { "active", 1 }, { "url", 1 } }),
        new(
            Key: "calendarevents_creation_desc",
            Collection: DbCollections.CalendarEvents,
            Name: "ix_calendarevents_creation_desc",
            Keys: new BsonDocument("creationDateTime", -1))
    ];

    internal static readonly IReadOnlyList<MongoIndexRemovalEntry> RemovalManifest =
    [
        new(
            Key: "pages_url",
            Collection: DbCollections.Pages,
            Name: "ix_pages_url",
            ReplacementKey: "pages_url.unique")
    ];

    public async Task<IList<MongoIndexAuditItem>> AuditAsync(IList<string>? keys = null, CancellationToken cancellationToken = default)
    {
        var selected = FilterManifest(keys, rejectUnknownKeys: false);
        var results = new List<MongoIndexAuditItem>(selected.Count);

        foreach (var entry in selected)
        {
            var collection = database.GetCollection<BsonDocument>(entry.Collection);
            var cursor = await collection.Indexes.ListAsync(cancellationToken);
            var indexDocs = await cursor.ToListAsync(cancellationToken);

            var exists = indexDocs.Any(x => x.GetValue("name", string.Empty).AsString == entry.Name);
            results.Add(new MongoIndexAuditItem(entry.Key, entry.Collection, entry.Name, exists, entry.Keys.ToJson()));
        }

        return results;
    }

    public async Task<IList<MongoIndexApplyResult>> ApplyAsync(IList<string> approvedKeys, CancellationToken cancellationToken = default)
    {
        var selected = FilterManifest(approvedKeys, rejectUnknownKeys: true);
        var audit = await AuditAsync(approvedKeys, cancellationToken);
        var auditMap = audit.ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);
        var results = new List<MongoIndexApplyResult>(selected.Count);

        foreach (var entry in selected)
        {
            var collection = database.GetCollection<BsonDocument>(entry.Collection);
            if (auditMap.TryGetValue(entry.Key, out var item) && item.Exists)
            {
                results.Add(new MongoIndexApplyResult(entry.Key, entry.Collection, entry.Name, "skipped_existing"));
                continue;
            }

            var options = new CreateIndexOptions<BsonDocument>
            {
                Name = entry.Name,
                Unique = entry.Unique,
                PartialFilterExpression = entry.PartialFilter == null
                    ? null
                    : new BsonDocumentFilterDefinition<BsonDocument>(entry.PartialFilter)
            };
            var model = new CreateIndexModel<BsonDocument>(entry.Keys, options);
            try
            {
                await collection.Indexes.CreateOneAsync(model, cancellationToken: cancellationToken);
            }
            catch (MongoException exception)
            {
                throw CreateOperationException($"Could not apply Mongo index '{entry.Key}'.", exception);
            }

            results.Add(new MongoIndexApplyResult(entry.Key, entry.Collection, entry.Name, "created"));
        }

        return results;
    }

    public async Task<IList<MongoIndexRemovalResult>> RemoveAsync(
        IList<string> approvedRemovalKeys,
        CancellationToken cancellationToken = default)
    {
        var selected = FilterRemovalManifest(approvedRemovalKeys);
        var results = new List<MongoIndexRemovalResult>(selected.Count);

        foreach (var removal in selected)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var replacement = Manifest.Single(entry =>
                string.Equals(entry.Key, removal.ReplacementKey, StringComparison.OrdinalIgnoreCase));
            var collection = database.GetCollection<BsonDocument>(removal.Collection);
            var cursor = await collection.Indexes.ListAsync(cancellationToken);
            var indexDocs = await cursor.ToListAsync(cancellationToken);

            if (!TryGetIndex(indexDocs, replacement.Name, out var replacementIndex) ||
                !HasExpectedDefinition(replacementIndex, replacement))
            {
                throw new MongoIndexOperationValidationException(
                    $"Cannot remove '{removal.Key}': replacement index '{replacement.Key}' must exist as unique {{ url: 1 }}.");
            }

            var removalAction = GetRemovalAction(indexDocs, removal);
            if (removalAction == "skipped_absent")
            {
                results.Add(new MongoIndexRemovalResult(
                    removal.Key,
                    removal.Collection,
                    removal.Name,
                    "skipped_absent"));
                continue;
            }

            try
            {
                await collection.Indexes.DropOneAsync(removal.Name, cancellationToken);
                results.Add(new MongoIndexRemovalResult(
                    removal.Key,
                    removal.Collection,
                    removal.Name,
                    "removed"));
            }
            catch (MongoCommandException exception) when (IsIndexNotFound(exception))
            {
                results.Add(new MongoIndexRemovalResult(
                    removal.Key,
                    removal.Collection,
                    removal.Name,
                    "skipped_absent"));
            }
            catch (MongoException exception)
            {
                throw CreateOperationException($"Could not remove Mongo index '{removal.Key}'.", exception);
            }
        }

        return results;
    }

    internal static bool HasExpectedDefinition(BsonDocument indexDocument, MongoIndexManifestEntry expected)
    {
        return indexDocument.GetValue("unique", false).ToBoolean() &&
            indexDocument.TryGetValue("key", out var actualKeys) &&
            actualKeys.IsBsonDocument &&
            actualKeys.AsBsonDocument.Equals(expected.Keys);
    }

    internal static string GetRemovalAction(
        IEnumerable<BsonDocument> indexDocuments,
        MongoIndexRemovalEntry removal)
    {
        return TryGetIndex(indexDocuments, removal.Name, out _)
            ? "removed"
            : "skipped_absent";
    }

    private static List<MongoIndexManifestEntry> FilterManifest(IList<string>? keys, bool rejectUnknownKeys)
    {
        if (keys == null || keys.Count == 0)
        {
            return [.. Manifest];
        }

        var keySet = keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (rejectUnknownKeys)
        {
            var unknownKeys = keySet
                .Where(key => Manifest.All(entry => !string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (unknownKeys.Length > 0)
            {
                throw new MongoIndexOperationValidationException(
                    $"Unknown Mongo index key(s): {string.Join(", ", unknownKeys)}.");
            }
        }

        return Manifest.Where(x => keySet.Contains(x.Key)).ToList();
    }

    private static List<MongoIndexRemovalEntry> FilterRemovalManifest(IList<string>? keys)
    {
        if (keys == null || keys.Count == 0)
        {
            throw new MongoIndexOperationValidationException("At least one approved removal key is required.");
        }

        var keySet = keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unknownKeys = keySet
            .Where(key => RemovalManifest.All(entry => !string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (unknownKeys.Length > 0)
        {
            throw new MongoIndexOperationValidationException(
                $"Unknown Mongo index removal key(s): {string.Join(", ", unknownKeys)}.");
        }

        return RemovalManifest.Where(x => keySet.Contains(x.Key)).ToList();
    }

    private static bool TryGetIndex(
        IEnumerable<BsonDocument> indexDocuments,
        string name,
        out BsonDocument indexDocument)
    {
        indexDocument = indexDocuments.FirstOrDefault(document =>
            document.GetValue("name", string.Empty).AsString == name)!;
        return indexDocument is not null;
    }

    private static bool IsIndexNotFound(MongoCommandException exception) =>
        exception.Code == 27 || string.Equals(exception.CodeName, "IndexNotFound", StringComparison.OrdinalIgnoreCase);

    private static MongoIndexOperationException CreateOperationException(string message, MongoException exception) =>
        new($"{message} MongoDB rejected the operation: {exception.Message}", exception);
}
