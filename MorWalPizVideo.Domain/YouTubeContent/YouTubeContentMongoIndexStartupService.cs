using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MorWalPizVideo.Models.Constraints;

namespace MorWalPizVideo.Domain.ContentIndexes;

public sealed record YouTubeContentMongoIndexDefinition(
    string Key,
    string Collection,
    string Name,
    BsonDocument Keys,
    bool Unique = false);

public static class YouTubeContentMongoIndexes
{
    public static readonly IReadOnlyList<YouTubeContentMongoIndexDefinition> Manifest =
    [
        new(
            "youtubecontent_latestpublished_creation_desc",
            DbCollections.YouTubeContent,
            "ix_youtubecontent_latestpublished_creation_desc",
            new BsonDocument
            {
                { "latestPublishedAt", -1 },
                { "creationDateTime", -1 }
            }),
        new(
            "youtubecontent_isprivate_latestpublished_creation_desc",
            DbCollections.YouTubeContent,
            "ix_youtubecontent_isprivate_latestpublished_creation_desc",
            new BsonDocument
            {
                { "isPrivate", 1 },
                { "latestPublishedAt", -1 },
                { "creationDateTime", -1 }
            })
    ];

    public static async Task EnsureAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        foreach (var definition in Manifest)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var collection = database.GetCollection<BsonDocument>(definition.Collection);
            var indexes = await ListIndexesAsync(collection, cancellationToken);
            var existing = indexes.FirstOrDefault(index =>
                index.GetValue("name", string.Empty).AsString == definition.Name);

            if (existing is not null)
            {
                if (!MatchesExpectedIndex(existing, definition))
                    throw new InvalidOperationException(
                        $"Mongo index '{definition.Name}' on '{definition.Collection}' exists with a different definition.");
                continue;
            }

            if (indexes.Any(index =>
                    index.TryGetValue("key", out var keys) &&
                    keys.IsBsonDocument &&
                    keys.AsBsonDocument.Equals(definition.Keys)))
            {
                throw new InvalidOperationException(
                    $"Mongo index '{definition.Collection}' already has the required key pattern under a different name.");
            }

            if (indexes.Count == 0)
                await EnsureCollectionExistsAsync(database, definition.Collection, cancellationToken);

            try
            {
                await collection.Indexes.CreateOneAsync(
                    new CreateIndexModel<BsonDocument>(
                        definition.Keys,
                        new CreateIndexOptions<BsonDocument>
                        {
                            Name = definition.Name,
                            Unique = definition.Unique
                        }),
                    cancellationToken: cancellationToken);
            }
            catch (MongoCommandException exception) when (exception.Code is 68 or 85 or 86)
            {
                indexes = await ListIndexesAsync(collection, cancellationToken);
                existing = indexes.FirstOrDefault(index =>
                    index.GetValue("name", string.Empty).AsString == definition.Name);
                if (existing is null || !MatchesExpectedIndex(existing, definition))
                    throw new InvalidOperationException(
                        $"Mongo index '{definition.Key}' could not be created for '{definition.Collection}'.",
                        exception);
            }
            catch (MongoException exception)
            {
                throw new InvalidOperationException(
                    $"Mongo index '{definition.Key}' could not be created for '{definition.Collection}'.",
                    exception);
            }
        }
    }

    internal static bool MatchesExpectedIndex(
        BsonDocument index,
        YouTubeContentMongoIndexDefinition expected) =>
        index.TryGetValue("key", out var keys) &&
        keys.IsBsonDocument &&
        keys.AsBsonDocument.Equals(expected.Keys) &&
        index.GetValue("unique", false).ToBoolean() == expected.Unique;

    private static async Task<List<BsonDocument>> ListIndexesAsync(
        IMongoCollection<BsonDocument> collection,
        CancellationToken cancellationToken)
    {
        try
        {
            var cursor = await collection.Indexes.ListAsync(cancellationToken);
            return await cursor.ToListAsync(cancellationToken);
        }
        catch (MongoCommandException exception) when (
            exception.Code == 26 ||
            string.Equals(exception.CodeName, "NamespaceNotFound", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }
    }

    private static async Task EnsureCollectionExistsAsync(
        IMongoDatabase database,
        string collectionName,
        CancellationToken cancellationToken)
    {
        var cursor = await database.ListCollectionsAsync(
            new ListCollectionsOptions { Filter = new BsonDocument("name", collectionName) },
            cancellationToken);
        if (await cursor.AnyAsync(cancellationToken))
            return;

        try
        {
            await database.CreateCollectionAsync(collectionName, cancellationToken: cancellationToken);
        }
        catch (MongoCommandException exception) when (exception.Code == 48)
        {
        }
    }
}

public sealed class YouTubeContentMongoIndexStartupService(
    IServiceScopeFactory scopeFactory,
    ILogger<YouTubeContentMongoIndexStartupService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
        await YouTubeContentMongoIndexes.EnsureAsync(database, cancellationToken);
        logger.LogInformation(
            "Verified {IndexCount} YouTube content Mongo indexes before accepting traffic",
            YouTubeContentMongoIndexes.Manifest.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
