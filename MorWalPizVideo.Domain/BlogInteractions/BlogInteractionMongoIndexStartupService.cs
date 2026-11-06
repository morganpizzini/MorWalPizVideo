using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MorWalPizVideo.Models.Constraints;

namespace MorWalPizVideo.Domain.BlogInteractions;

public sealed record BlogInteractionMongoIndexDefinition(
    string Key,
    string Collection,
    string Name,
    BsonDocument Keys,
    bool Unique = false);

public static class BlogInteractionMongoIndexes
{
    public static readonly IReadOnlyList<BlogInteractionMongoIndexDefinition> Manifest =
    [
        new(
            "blogcomments.channel_account_request.unique",
            DbCollections.BlogComments,
            "ux_blogcomments_channel_account_request",
            new BsonDocument { { "channelId", 1 }, { "accountId", 1 }, { "requestId", 1 } },
            Unique: true),
        new(
            "blogcomments.channel_post_status_created",
            DbCollections.BlogComments,
            "ix_blogcomments_channel_post_status_created",
            new BsonDocument { { "channelId", 1 }, { "postId", 1 }, { "status", 1 }, { "creationDateTime", -1 } }),
        new(
            "blogcomments.channel_status_created",
            DbCollections.BlogComments,
            "ix_blogcomments_channel_status_created",
            new BsonDocument { { "channelId", 1 }, { "status", 1 }, { "creationDateTime", -1 } }),
        new(
            "blogcomments.channel_account_created",
            DbCollections.BlogComments,
            "ix_blogcomments_channel_account_created",
            new BsonDocument { { "channelId", 1 }, { "accountId", 1 }, { "creationDateTime", -1 } }),
        new(
            "blogreactions.channel_post_account.unique",
            DbCollections.BlogReactions,
            "ux_blogreactions_channel_post_account",
            new BsonDocument { { "channelId", 1 }, { "postId", 1 }, { "accountId", 1 } },
            Unique: true),
        new(
            "blogreactions.channel_account_created",
            DbCollections.BlogReactions,
            "ix_blogreactions_channel_account_created",
            new BsonDocument { { "channelId", 1 }, { "accountId", 1 }, { "creationDateTime", -1 } }),
        new(
            "blogportalaccess.channel_account.unique",
            DbCollections.BlogPortalAccess,
            "ux_blogportalaccess_channel_account",
            new BsonDocument { { "channelId", 1 }, { "accountId", 1 } },
            Unique: true)
    ];

    public static async Task EnsureAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        foreach (var definition in Manifest)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var collection = database.GetCollection<BsonDocument>(definition.Collection);
            var indexes = await ListIndexesAsync(collection, cancellationToken);

            var existingIndex = indexes.FirstOrDefault(index =>
                index.GetValue("name", string.Empty).AsString == definition.Name);
            if (existingIndex is not null)
            {
                if (!MatchesExpectedIndex(existingIndex, definition))
                    throw new InvalidOperationException(
                        $"Mongo index '{definition.Name}' on '{definition.Collection}' exists with a different definition. " +
                        "Resolve the conflict through the reviewed Mongo index operation.");

                continue;
            }

            if (indexes.Any(index =>
                    index.TryGetValue("key", out var keys) &&
                    keys.IsBsonDocument &&
                    keys.AsBsonDocument.Equals(definition.Keys)))
            {
                throw new InvalidOperationException(
                    $"Mongo index '{definition.Collection}' already has the required key pattern under a different name. " +
                    "Resolve the index name conflict through the reviewed Mongo index operation.");
            }

            if (indexes.Count == 0)
                await EnsureCollectionExistsAsync(database, definition.Collection, cancellationToken);

            var model = new CreateIndexModel<BsonDocument>(
                definition.Keys,
                new CreateIndexOptions<BsonDocument>
                {
                    Name = definition.Name,
                    Unique = definition.Unique
                });

            try
            {
                await collection.Indexes.CreateOneAsync(model, cancellationToken: cancellationToken);
            }
            catch (MongoCommandException exception) when (IsIndexDefinitionConflict(exception))
            {
                indexes = await ListIndexesAsync(collection, cancellationToken);
                existingIndex = indexes.FirstOrDefault(index =>
                    index.GetValue("name", string.Empty).AsString == definition.Name);
                if (existingIndex is not null && MatchesExpectedIndex(existingIndex, definition))
                    continue;

                throw new InvalidOperationException(
                    $"Mongo index '{definition.Key}' could not be created for '{definition.Collection}'. " +
                    "Inspect existing index definitions and data before retrying.",
                    exception);
            }
            catch (MongoException exception)
            {
                throw new InvalidOperationException(
                    $"Mongo index '{definition.Key}' could not be created for '{definition.Collection}'. " +
                    "Inspect existing data and index definitions before retrying.",
                    exception);
            }
        }
    }

    internal static bool IsNamespaceNotFound(int code, string? codeName) =>
        code == 26 || string.Equals(codeName, "NamespaceNotFound", StringComparison.OrdinalIgnoreCase);

    private static async Task<List<BsonDocument>> ListIndexesAsync(
        IMongoCollection<BsonDocument> collection,
        CancellationToken cancellationToken)
    {
        try
        {
            var cursor = await collection.Indexes.ListAsync(cancellationToken);
            return await cursor.ToListAsync(cancellationToken);
        }
        catch (MongoCommandException exception) when (IsNamespaceNotFound(exception.Code, exception.CodeName))
        {
            return [];
        }
    }

    private static async Task EnsureCollectionExistsAsync(
        IMongoDatabase database,
        string collectionName,
        CancellationToken cancellationToken)
    {
        try
        {
            var cursor = await database.ListCollectionsAsync(
                new ListCollectionsOptions { Filter = new BsonDocument("name", collectionName) },
                cancellationToken);
            if (await cursor.AnyAsync(cancellationToken))
                return;

            await database.CreateCollectionAsync(collectionName, cancellationToken: cancellationToken);
        }
        catch (MongoCommandException exception) when (exception.Code == 48)
        {
            // Another API instance created the same empty collection during startup.
        }
    }

    private static bool MatchesExpectedIndex(
        BsonDocument index,
        BlogInteractionMongoIndexDefinition expected) =>
        index.TryGetValue("key", out var keys) &&
            keys.IsBsonDocument &&
            keys.AsBsonDocument.Equals(expected.Keys) &&
            index.GetValue("unique", false).ToBoolean() == expected.Unique;

    private static bool IsIndexDefinitionConflict(MongoCommandException exception) =>
        exception.Code is 68 or 85 or 86;
}

public sealed class BlogInteractionMongoIndexStartupService(
    IServiceScopeFactory scopeFactory,
    ILogger<BlogInteractionMongoIndexStartupService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
        await BlogInteractionMongoIndexes.EnsureAsync(database, cancellationToken);
        logger.LogInformation(
            "Verified {IndexCount} blog interaction Mongo indexes before accepting traffic",
            BlogInteractionMongoIndexes.Manifest.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
