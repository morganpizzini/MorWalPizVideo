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

public interface IMongoIndexOperationsService
{
    Task<IList<MongoIndexAuditItem>> AuditAsync(IList<string>? keys = null, CancellationToken cancellationToken = default);
    Task<IList<MongoIndexApplyResult>> ApplyAsync(IList<string> approvedKeys, CancellationToken cancellationToken = default);
}

public sealed class MongoIndexOperationsService(IMongoDatabase database) : IMongoIndexOperationsService
{
    private static readonly IReadOnlyList<MongoIndexManifestEntry> Manifest =
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
            Key: "pages_url",
            Collection: DbCollections.Pages,
            Name: "ix_pages_url",
            Keys: new BsonDocument("url", 1)),
        new(
            Key: "compilations_url",
            Collection: DbCollections.Compilations,
            Name: "ix_compilations_url",
            Keys: new BsonDocument("url", 1)),
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

    public async Task<IList<MongoIndexAuditItem>> AuditAsync(IList<string>? keys = null, CancellationToken cancellationToken = default)
    {
        var selected = FilterManifest(keys);
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
        var selected = FilterManifest(approvedKeys);
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
            await collection.Indexes.CreateOneAsync(model, cancellationToken: cancellationToken);
            results.Add(new MongoIndexApplyResult(entry.Key, entry.Collection, entry.Name, "created"));
        }

        return results;
    }

    private static List<MongoIndexManifestEntry> FilterManifest(IList<string>? keys)
    {
        if (keys == null || keys.Count == 0)
        {
            return [.. Manifest];
        }

        var keySet = keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return Manifest.Where(x => keySet.Contains(x.Key)).ToList();
    }
}
