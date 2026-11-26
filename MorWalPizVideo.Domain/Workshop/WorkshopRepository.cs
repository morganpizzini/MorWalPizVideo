using MongoDB.Bson;
using MongoDB.Driver;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models.Workshop;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Domain.Workshop;

public interface IWorkshopRepository
{
    Task<IReadOnlyList<T>> ListAsync<T>(string collection, string accountId, CancellationToken cancellationToken = default)
        where T : BaseEntity;
    Task<IReadOnlyList<T>> ListByDateAsync<T>(string collection, string accountId, string dateField,
        DateTime fromInclusive, DateTime toExclusive, CancellationToken cancellationToken = default)
        where T : BaseEntity;
    Task<IReadOnlyList<WorkshopRecipeJournalEntry>> ListRecipeJournalEntriesAsync(
        string accountId, string recipeId, CancellationToken cancellationToken = default);
    Task<T?> GetAsync<T>(string collection, string accountId, string id, CancellationToken cancellationToken = default)
        where T : BaseEntity;
    Task AddAsync<T>(string collection, T item, CancellationToken cancellationToken = default)
        where T : BaseEntity;
    Task AddRecipeJournalEntryAsync(
        WorkshopRecipeJournalEntry entry, CancellationToken cancellationToken = default);
    Task<bool> ReplaceAsync<T>(string collection, T item, string accountId, long? expectedRevision = null,
        CancellationToken cancellationToken = default) where T : BaseEntity;
    Task<bool> DeleteAsync<T>(string collection, string accountId, string id, long? expectedRevision = null,
        CancellationToken cancellationToken = default) where T : BaseEntity;
    Task<WorkshopOperation?> FindOperationAsync(string accountId, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<WorkshopOperation?> GetOperationAsync(string accountId, string operationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkshopOperation>> ListOperationsByTypeAsync(string accountId,
        WorkshopOperationType operationType, CancellationToken cancellationToken = default);
    Task AddOperationAsync(WorkshopOperation operation, CancellationToken cancellationToken = default);
    Task<WorkshopReconciliationState?> GetReconciliationAsync(string accountId, CancellationToken cancellationToken = default);
    Task SaveReconciliationAsync(WorkshopReconciliationState state, CancellationToken cancellationToken = default);
}

public sealed class WorkshopRepository(IMongoDatabase database) : IWorkshopRepository
{
    public async Task<IReadOnlyList<T>> ListAsync<T>(string collection, string accountId, CancellationToken cancellationToken = default)
        where T : BaseEntity =>
        await database.GetCollection<T>(collection)
            .Find(Builders<T>.Filter.Eq("accountId", accountId))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<T>> ListByDateAsync<T>(string collection, string accountId, string dateField,
        DateTime fromInclusive, DateTime toExclusive, CancellationToken cancellationToken = default)
        where T : BaseEntity =>
        await database.GetCollection<T>(collection)
            .Find(Builders<T>.Filter.And(
                Builders<T>.Filter.Eq("accountId", accountId),
                Builders<T>.Filter.Gte(dateField, fromInclusive),
                Builders<T>.Filter.Lt(dateField, toExclusive)))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<WorkshopRecipeJournalEntry>> ListRecipeJournalEntriesAsync(
        string accountId, string recipeId, CancellationToken cancellationToken = default) =>
        await database.GetCollection<WorkshopRecipeJournalEntry>(DbCollections.WorkshopRecipeJournalEntries)
            .Find(x => x.AccountId == accountId && x.RecipeId == recipeId)
            .SortByDescending(x => x.EntryDate)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

    public Task<T?> GetAsync<T>(string collection, string accountId, string id, CancellationToken cancellationToken = default)
        where T : BaseEntity =>
        database.GetCollection<T>(collection)
            .Find(Builders<T>.Filter.And(Builders<T>.Filter.Eq("accountId", accountId), Builders<T>.Filter.Eq(entity => entity.Id, id)))
            .FirstOrDefaultAsync(cancellationToken);

    public Task AddAsync<T>(string collection, T item, CancellationToken cancellationToken = default)
        where T : BaseEntity =>
        database.GetCollection<T>(collection).InsertOneAsync(item, cancellationToken: cancellationToken);

    public async Task AddRecipeJournalEntryAsync(
        WorkshopRecipeJournalEntry entry, CancellationToken cancellationToken = default)
    {
        try
        {
            await database.GetCollection<WorkshopRecipeJournalEntry>(DbCollections.WorkshopRecipeJournalEntries)
                .InsertOneAsync(entry, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new WorkshopDuplicateRecipeJournalException();
        }
    }

    public async Task<bool> ReplaceAsync<T>(string collection, T item, string accountId, long? expectedRevision = null,
        CancellationToken cancellationToken = default) where T : BaseEntity
    {
        var filter = Builders<T>.Filter.And(
            Builders<T>.Filter.Eq("accountId", accountId),
            Builders<T>.Filter.Eq(entity => entity.Id, item.Id));
        if (expectedRevision.HasValue)
            filter &= Builders<T>.Filter.Eq("revision", expectedRevision.Value);
        return (await database.GetCollection<T>(collection).ReplaceOneAsync(filter, item, cancellationToken: cancellationToken)).ModifiedCount == 1;
    }

    public async Task<bool> DeleteAsync<T>(string collection, string accountId, string id, long? expectedRevision = null,
        CancellationToken cancellationToken = default) where T : BaseEntity
    {
        var filter = Builders<T>.Filter.And(
            Builders<T>.Filter.Eq("accountId", accountId),
            Builders<T>.Filter.Eq(entity => entity.Id, id));
        if (expectedRevision.HasValue)
            filter &= Builders<T>.Filter.Eq("revision", expectedRevision.Value);
        return (await database.GetCollection<T>(collection).DeleteOneAsync(filter, cancellationToken)).DeletedCount == 1;
    }

    public Task<WorkshopOperation?> FindOperationAsync(string accountId, string idempotencyKey, CancellationToken cancellationToken = default) =>
        database.GetCollection<WorkshopOperation>(DbCollections.WorkshopOperations)
            .Find(x => x.AccountId == accountId && x.ClientIdempotencyKey == idempotencyKey)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<WorkshopOperation?> GetOperationAsync(string accountId, string operationId, CancellationToken cancellationToken = default) =>
        database.GetCollection<WorkshopOperation>(DbCollections.WorkshopOperations)
            .Find(x => x.AccountId == accountId && x.Id == operationId)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<WorkshopOperation>> ListOperationsByTypeAsync(string accountId,
        WorkshopOperationType operationType, CancellationToken cancellationToken = default) =>
        await database.GetCollection<WorkshopOperation>(DbCollections.WorkshopOperations)
            .Find(x => x.AccountId == accountId && x.OperationType == operationType)
            .ToListAsync(cancellationToken);

    public Task AddOperationAsync(WorkshopOperation operation, CancellationToken cancellationToken = default) =>
        database.GetCollection<WorkshopOperation>(DbCollections.WorkshopOperations)
            .InsertOneAsync(operation, cancellationToken: cancellationToken);

    public Task<WorkshopReconciliationState?> GetReconciliationAsync(string accountId, CancellationToken cancellationToken = default) =>
        database.GetCollection<WorkshopReconciliationState>(DbCollections.WorkshopReconciliation)
            .Find(x => x.AccountId == accountId).FirstOrDefaultAsync(cancellationToken);

    public async Task SaveReconciliationAsync(WorkshopReconciliationState state, CancellationToken cancellationToken = default)
    {
        await database.GetCollection<WorkshopReconciliationState>(DbCollections.WorkshopReconciliation)
            .ReplaceOneAsync(x => x.AccountId == state.AccountId, state, new ReplaceOptions { IsUpsert = true }, cancellationToken);
    }
}

public sealed class WorkshopMockRepository(IMockScenario scenario) : IWorkshopRepository
{
    private readonly object gate = new();

    public Task<IReadOnlyList<T>> ListAsync<T>(string collection, string accountId, CancellationToken cancellationToken = default)
        where T : BaseEntity
    {
        lock (gate) return Task.FromResult<IReadOnlyList<T>>(scenario.Read<T>(collection).Where(x => ReadAccountId(x) == accountId).ToArray());
    }

    public Task<IReadOnlyList<T>> ListByDateAsync<T>(string collection, string accountId, string dateField,
        DateTime fromInclusive, DateTime toExclusive, CancellationToken cancellationToken = default)
        where T : BaseEntity
    {
        lock (gate)
        {
            var items = scenario.Read<T>(collection).Where(x => ReadAccountId(x) == accountId)
                .Where(x => ReadDate(x, dateField) is { } date && date >= fromInclusive && date < toExclusive)
                .ToArray();
            return Task.FromResult<IReadOnlyList<T>>(items);
        }
    }

    public Task<IReadOnlyList<WorkshopRecipeJournalEntry>> ListRecipeJournalEntriesAsync(
        string accountId, string recipeId, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var entries = scenario.Read<WorkshopRecipeJournalEntry>(DbCollections.WorkshopRecipeJournalEntries)
                .Where(x => x.AccountId == accountId && x.RecipeId == recipeId)
                .OrderByDescending(x => x.EntryDate, StringComparer.Ordinal)
                .ThenByDescending(x => x.Id, StringComparer.Ordinal)
                .ToArray();
            return Task.FromResult<IReadOnlyList<WorkshopRecipeJournalEntry>>(entries);
        }
    }

    public Task<T?> GetAsync<T>(string collection, string accountId, string id, CancellationToken cancellationToken = default)
        where T : BaseEntity
    {
        lock (gate) return Task.FromResult(scenario.Read<T>(collection).FirstOrDefault(x => x.Id == id && ReadAccountId(x) == accountId));
    }

    public Task AddAsync<T>(string collection, T item, CancellationToken cancellationToken = default) where T : BaseEntity
    {
        lock (gate) scenario.Add(collection, item);
        return Task.CompletedTask;
    }

    public Task AddRecipeJournalEntryAsync(
        WorkshopRecipeJournalEntry entry, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            if (scenario.Read<WorkshopRecipeJournalEntry>(DbCollections.WorkshopRecipeJournalEntries)
                .Any(x => x.Id == entry.Id))
                throw new WorkshopDuplicateRecipeJournalException();
            scenario.Add(DbCollections.WorkshopRecipeJournalEntries, entry);
        }
        return Task.CompletedTask;
    }

    public Task<bool> ReplaceAsync<T>(string collection, T item, string accountId, long? expectedRevision = null,
        CancellationToken cancellationToken = default) where T : BaseEntity
    {
        lock (gate)
        {
            var old = scenario.Read<T>(collection).FirstOrDefault(x => x.Id == item.Id && ReadAccountId(x) == accountId);
            if (old is null || expectedRevision.HasValue && ReadRevision(old) != expectedRevision.Value) return Task.FromResult(false);
            scenario.Replace(collection, item);
            return Task.FromResult(true);
        }
    }

    public Task<bool> DeleteAsync<T>(string collection, string accountId, string id, long? expectedRevision = null,
        CancellationToken cancellationToken = default) where T : BaseEntity
    {
        lock (gate)
        {
            var old = scenario.Read<T>(collection).FirstOrDefault(x => x.Id == id && ReadAccountId(x) == accountId);
            if (old is null || expectedRevision.HasValue && ReadRevision(old) != expectedRevision.Value)
                return Task.FromResult(false);
            scenario.Delete<T>(collection, id);
            return Task.FromResult(true);
        }
    }

    public Task<WorkshopOperation?> FindOperationAsync(string accountId, string idempotencyKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(scenario.Read<WorkshopOperation>(DbCollections.WorkshopOperations)
            .FirstOrDefault(x => x.AccountId == accountId && x.ClientIdempotencyKey == idempotencyKey));

    public Task<WorkshopOperation?> GetOperationAsync(string accountId, string operationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(scenario.Read<WorkshopOperation>(DbCollections.WorkshopOperations)
            .FirstOrDefault(x => x.AccountId == accountId && x.Id == operationId));

    public Task<IReadOnlyList<WorkshopOperation>> ListOperationsByTypeAsync(string accountId,
        WorkshopOperationType operationType, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<WorkshopOperation>>(scenario.Read<WorkshopOperation>(DbCollections.WorkshopOperations)
            .Where(x => x.AccountId == accountId && x.OperationType == operationType).ToArray());

    public Task AddOperationAsync(WorkshopOperation operation, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            if (scenario.Read<WorkshopOperation>(DbCollections.WorkshopOperations).Any(x =>
                    x.AccountId == operation.AccountId && x.ClientIdempotencyKey == operation.ClientIdempotencyKey))
                throw new WorkshopDuplicateOperationException();
            scenario.Add(DbCollections.WorkshopOperations, operation);
        }
        return Task.CompletedTask;
    }

    public Task<WorkshopReconciliationState?> GetReconciliationAsync(string accountId, CancellationToken cancellationToken = default) =>
        Task.FromResult(scenario.Read<WorkshopReconciliationState>(DbCollections.WorkshopReconciliation)
            .FirstOrDefault(x => x.AccountId == accountId));

    public Task SaveReconciliationAsync(WorkshopReconciliationState state, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var existing = scenario.Read<WorkshopReconciliationState>(DbCollections.WorkshopReconciliation)
                .FirstOrDefault(x => x.AccountId == state.AccountId);
            if (existing is null) scenario.Add(DbCollections.WorkshopReconciliation, state);
            else scenario.Replace(DbCollections.WorkshopReconciliation, state);
        }
        return Task.CompletedTask;
    }

    private static string? ReadAccountId<T>(T value) where T : BaseEntity =>
        value switch
        {
            Firearm x => x.AccountId,
            ComponentDefinition x => x.AccountId,
            ComponentInstance x => x.AccountId,
            ComponentInstallation x => x.AccountId,
            MaintenanceEvent x => x.AccountId,
            AmmoRecipe x => x.AccountId,
            AmmoRecipeVersion x => x.AccountId,
            WorkshopRecipeJournalEntry x => x.AccountId,
            ProductionSession x => x.AccountId,
            AmmoBatch x => x.AccountId,
            WorkshopSupplyProduct x => x.AccountId,
            WorkshopSupplyPurchase x => x.AccountId,
            WorkshopSupplyLot x => x.AccountId,
            WorkshopComponentPurchase x => x.AccountId,
            AmmoAllocation x => x.AccountId,
            ShootingSession x => x.AccountId,
            WorkshopOperation x => x.AccountId,
            WorkshopActivity x => x.AccountId,
            WorkshopReconciliationState x => x.AccountId,
            _ => null
        };

    private static long? ReadRevision<T>(T value) where T : BaseEntity =>
        value switch
        {
            Firearm x => x.Revision,
            ComponentDefinition x => x.Revision,
            ComponentInstance x => x.Revision,
            ComponentInstallation x => x.Revision,
            ProductionSession x => x.Revision,
            AmmoBatch x => x.Revision,
            AmmoAllocation x => x.Revision,
            WorkshopSupplyProduct x => x.Revision,
            WorkshopSupplyLot x => x.Revision,
            WorkshopComponentPurchase x => x.Revision,
            _ => null
        };

    private static DateTime? ReadDate<T>(T value, string dateField) where T : BaseEntity =>
        (value, dateField) switch
        {
            (WorkshopOperation x, "effectiveAt") => x.EffectiveAt,
            (WorkshopActivity x, "effectiveAt") => x.EffectiveAt,
            (ShootingSession x, "date") => x.Date,
            (ProductionSession x, "productionDate") => x.ProductionDate,
            (AmmoBatch x, "productionDate") => x.ProductionDate,
            (MaintenanceEvent x, "date") => x.Date,
            _ => null
        };
}

public sealed class WorkshopDuplicateOperationException : Exception;
public sealed class WorkshopDuplicateRecipeJournalException : Exception;

public sealed record WorkshopMongoIndexDefinition(string Key, string Collection, string Name, BsonDocument Keys, bool Unique = false);

public static class WorkshopMongoIndexes
{
    public static readonly IReadOnlyList<WorkshopMongoIndexDefinition> Manifest =
    [
        new("operations.account_idempotency.unique", DbCollections.WorkshopOperations, "ux_workshop_operations_account_idempotency",
            new BsonDocument { { "accountId", 1 }, { "clientIdempotencyKey", 1 } }, true),
        new("operations.account_aggregate_order", DbCollections.WorkshopOperations, "ix_workshop_operations_account_aggregate_order",
            new BsonDocument { { "accountId", 1 }, { "deltas.aggregateKind", 1 }, { "deltas.aggregateId", 1 }, { "effectiveAt", 1 }, { "recordedAt", 1 } }),
        new("installations.account_component_active", DbCollections.WorkshopComponentInstallations, "ix_workshop_installations_account_component_active",
            new BsonDocument { { "accountId", 1 }, { "componentId", 1 }, { "removedAt", 1 } }),
        new("batches.account_recipe_date", DbCollections.WorkshopAmmoBatches, "ix_workshop_batches_account_recipe_date",
            new BsonDocument { { "accountId", 1 }, { "recipeId", 1 }, { "productionDate", -1 } }),
        new("recipejournal.account_recipe_date", DbCollections.WorkshopRecipeJournalEntries,
            "ix_workshop_recipejournal_account_recipe_date",
            new BsonDocument { { "accountId", 1 }, { "recipeId", 1 }, { "entryDate", -1 }, { "_id", -1 } }),
        new("supplyproducts.account_type", DbCollections.WorkshopSupplyProducts, "ix_workshop_supplyproducts_account_type",
            new BsonDocument { { "accountId", 1 }, { "type", 1 }, { "archived", 1 } }),
        new("supplypurchases.account_product_date", DbCollections.WorkshopSupplyPurchases, "ix_workshop_supplypurchases_account_product_date",
            new BsonDocument { { "accountId", 1 }, { "productId", 1 }, { "purchasedAt", -1 } }),
        new("supplylots.account_product_date", DbCollections.WorkshopSupplyLots, "ix_workshop_supplylots_account_product_date",
            new BsonDocument { { "accountId", 1 }, { "productId", 1 }, { "purchasedAt", 1 }, { "_id", 1 } }),
        new("componentpurchases.account_component.unique", DbCollections.WorkshopComponentPurchases,
            "ux_workshop_componentpurchases_account_component",
            new BsonDocument { { "accountId", 1 }, { "componentId", 1 } }, true),
        new("activities.account_date_type", DbCollections.WorkshopActivities, "ix_workshop_activities_account_date_type",
            new BsonDocument { { "accountId", 1 }, { "effectiveAt", -1 }, { "type", 1 } })
    ];
}
