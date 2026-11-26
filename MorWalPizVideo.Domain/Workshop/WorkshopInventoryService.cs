using System.Globalization;
using System.Text.Json;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models.Workshop;

namespace MorWalPizVideo.Domain.Workshop;

public sealed record WorkshopInventoryData(
    IReadOnlyList<WorkshopSupplyProduct> Products,
    IReadOnlyList<WorkshopSupplyLot> Lots,
    IReadOnlyList<WorkshopComponentPurchase> ComponentPurchases,
    IReadOnlyList<(string Currency, decimal Total)> Spending,
    IReadOnlyList<(string Currency, decimal Total)> PartialConsumptionCosts,
    IReadOnlyList<string> MissingComponentPrices);

public sealed record WorkshopProductionMutationResult(
    WorkshopOperation Operation, ProductionSession Session, AmmoBatch Batch, bool Idempotent);

public sealed record WorkshopProductionRecordData(
    ProductionSession Session, IReadOnlyList<(WorkshopOperation Operation, WorkshopProductionCorrectionFact Correction)> Corrections,
    bool ProjectionPending);

public sealed record WorkshopProductionEditCommand(
    WorkshopProductionCommand Production, long ExpectedRevision);

public sealed partial class WorkshopService
{
    private const long MaximumSafeJavaScriptInteger = 9_007_199_254_740_991;
    private const long MaximumPackageCount = 10_000;

    public async Task<IReadOnlyList<WorkshopProductionRecordData>> GetProductionRecordsAsync(
        string accountId, CancellationToken ct = default)
    {
        var sessions = await repository.ListAsync<ProductionSession>(
            DbCollections.WorkshopProductionSessions, accountId, ct);
        var operations = await repository.ListAsync<WorkshopOperation>(
            DbCollections.WorkshopOperations, accountId, ct);
        var replay = WorkshopReplay.Rebuild(operations);
        var records = new List<WorkshopProductionRecordData>(sessions.Count);
        foreach (var session in sessions.OrderByDescending(x => x.ProductionDate).ThenByDescending(x => x.Id))
        {
            var related = operations.Where(x => x.SourceEntityIds.Contains(session.Id, StringComparer.Ordinal)).ToArray();
            var original = related.FirstOrDefault(x => x.OperationType == WorkshopOperationType.ProductionRecorded);
            var corrections = related.Where(x => x.OperationType == WorkshopOperationType.ProductionEdited &&
                    x.ProductionCorrection is not null)
                .OrderBy(x => x.RecordedAt).ThenBy(x => x.Id)
                .Select(x => (x, x.ProductionCorrection!)).ToArray();
            var batch = await repository.GetAsync<AmmoBatch>(
                DbCollections.WorkshopAmmoBatches, accountId, session.BatchId, ct);
            var pending = original is null || batch is null;
            if (corrections.Length > 0)
            {
                var correction = corrections[^1].Item2;
                pending |= session.Revision != correction.ExpectedRevision + 1 ||
                           session.ProductionDate != correction.ProductionDate ||
                           session.QuantityProduced != correction.Quantity ||
                           session.BulletProductId != correction.BulletProductId ||
                           session.Notes != correction.Notes ||
                           !session.PrimerLotAllocations.SequenceEqual(correction.PrimerLotAllocations) ||
                           !session.BulletLotAllocations.SequenceEqual(correction.BulletLotAllocations);
            }
            if (batch is not null)
            {
                var productionOperations = related.Where(x =>
                    x.OperationType is WorkshopOperationType.ProductionRecorded or WorkshopOperationType.ProductionEdited);
                foreach (var delta in productionOperations.SelectMany(x => x.Deltas)
                             .GroupBy(x => (x.AggregateKind, x.AggregateId)).Select(x => x.First()))
                {
                    var replayed = replay.For(delta.AggregateKind, delta.AggregateId);
                    if (delta.AggregateKind == WorkshopAggregateKind.Batch &&
                        delta.AggregateId == batch.Id && batch.OnHandQuantity != replayed.Stock)
                        pending = true;
                    if (delta.AggregateKind == WorkshopAggregateKind.SupplyLot)
                    {
                        var lot = await repository.GetAsync<WorkshopSupplyLot>(
                            DbCollections.WorkshopSupplyLots, accountId, delta.AggregateId, ct);
                        if (lot is null || lot.AvailableQuantity != replayed.Stock) pending = true;
                    }
                }
                pending |= batch.ProducedQuantity != session.QuantityProduced ||
                           batch.ProductionDate != session.ProductionDate ||
                           batch.BulletProductId != session.BulletProductId ||
                           batch.Notes != session.Notes ||
                           !batch.PrimerLotAllocations.SequenceEqual(session.PrimerLotAllocations) ||
                           !batch.BulletLotAllocations.SequenceEqual(session.BulletLotAllocations);
            }
            records.Add(new(session, corrections, pending));
        }
        return records;
    }

    public async Task<WorkshopInventoryData> GetInventoryAsync(string accountId, CancellationToken ct = default)
    {
        var products = await repository.ListAsync<WorkshopSupplyProduct>(DbCollections.WorkshopSupplyProducts, accountId, ct);
        var purchases = await repository.ListAsync<WorkshopSupplyPurchase>(DbCollections.WorkshopSupplyPurchases, accountId, ct);
        var lots = await repository.ListAsync<WorkshopSupplyLot>(DbCollections.WorkshopSupplyLots, accountId, ct);
        var componentPurchases = await repository.ListAsync<WorkshopComponentPurchase>(
            DbCollections.WorkshopComponentPurchases, accountId, ct);
        var operations = await repository.ListAsync<WorkshopOperation>(DbCollections.WorkshopOperations, accountId, ct);
        var components = await repository.ListAsync<ComponentInstance>(DbCollections.WorkshopComponentInstances, accountId, ct);
        var productById = products.ToDictionary(x => x.Id, StringComparer.Ordinal);
        var spending = new Dictionary<string, decimal>(StringComparer.Ordinal);
        foreach (var purchase in purchases)
            AddCurrency(spending, purchase.Currency, purchase.TotalPrice);
        foreach (var purchase in componentPurchases)
            AddCurrency(spending, purchase.Currency, purchase.TotalPrice);

        var costs = new Dictionary<string, decimal>(StringComparer.Ordinal);
        foreach (var purchase in purchases)
            if (productById.TryGetValue(purchase.ProductId, out var product) &&
                product.Type is WorkshopSupplyType.Powder or WorkshopSupplyType.Case)
                AddCurrency(costs, purchase.Currency, purchase.TotalPrice);
        var lotById = lots.ToDictionary(x => x.Id, StringComparer.Ordinal);
        var consumedByLot = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var fact in operations.SelectMany(x => x.InventoryFacts))
        {
            if (fact.LotId is null || !fact.CurrentAmount.HasValue ||
                !(fact.Type.EndsWith("-consumption", StringComparison.Ordinal) ||
                  fact.Type.StartsWith("reversed-", StringComparison.Ordinal))) continue;
            var sign = fact.Type.StartsWith("reversed-", StringComparison.Ordinal) ? -1 : 1;
            consumedByLot[fact.LotId] = consumedByLot.GetValueOrDefault(fact.LotId) +
                                        sign * decimal.ToInt64(fact.CurrentAmount.Value);
        }
        foreach (var (lotId, consumed) in consumedByLot)
        {
            if (consumed <= 0 || !lotById.TryGetValue(lotId, out var lot) ||
                !productById.TryGetValue(lot.ProductId, out var product) ||
                product.Type is not (WorkshopSupplyType.Primer or WorkshopSupplyType.Bullet)) continue;
            var attributed = Math.Min(consumed, lot.PurchasedQuantity);
            var cost = decimal.Round(lot.TotalPrice * attributed / lot.PurchasedQuantity, 2,
                MidpointRounding.AwayFromZero);
            AddCurrency(costs, lot.Currency, cost);
        }

        var pricedComponents = componentPurchases.Select(x => x.ComponentId).ToHashSet(StringComparer.Ordinal);
        return new(
            products.OrderBy(x => x.Type).ThenBy(x => x.Name, StringComparer.Ordinal).ThenBy(x => x.Id).ToArray(),
            lots.OrderBy(x => x.PurchasedAt).ThenBy(x => x.Id).ToArray(),
            componentPurchases.OrderBy(x => x.ComponentId, StringComparer.Ordinal).ToArray(),
            CurrencyTotals(spending), CurrencyTotals(costs),
            components.Where(x => !pricedComponents.Contains(x.Id)).Select(x => x.Id).Order(StringComparer.Ordinal).ToArray());
    }

    public async Task<WorkshopSupplyProduct> CreateSupplyProductAsync(
        string accountId, string type, string? brand, string name, string idempotencyKey, CancellationToken ct = default)
    {
        if (!Enum.TryParse<WorkshopSupplyType>(type, true, out var productType) ||
            !Enum.IsDefined(productType))
            throw new WorkshopValidationException("Supply type is not supported.");
        RequireText(name, "Product name", 160);
        RequireText(idempotencyKey, "Idempotency key", 80);
        idempotencyKey = idempotencyKey.Trim();
        var command = new { Type = productType, Brand = Trim(brand), Name = name.Trim(), IdempotencyKey = idempotencyKey.Trim() };
        var id = StableId(accountId, idempotencyKey, "supply-product");
        var sources = new[] { id };
        var payload = JsonSerializer.Serialize(command);
        var existing = await repository.FindOperationAsync(accountId, idempotencyKey, ct);
        if (existing is not null &&
            existing.PayloadHash != PayloadHash(WorkshopOperationType.SupplyProductCreated, [], sources, payload, null))
            throw new WorkshopConflictException("Idempotency key was already used for a different operation.");

        var product = await repository.GetAsync<WorkshopSupplyProduct>(
            DbCollections.WorkshopSupplyProducts, accountId, id, ct);
        if (product is null)
        {
            product = new WorkshopSupplyProduct
            {
                Id = id, AccountId = accountId, Type = productType, Brand = Trim(brand),
                Name = name.Trim(), CreationDateTime = Now()
            };
            await repository.AddAsync(DbCollections.WorkshopSupplyProducts, product, ct);
        }
        await CommitAsync(accountId, idempotencyKey.Trim(), WorkshopOperationType.SupplyProductCreated,
            Now(), [], sources, payload, ct);
        return product;
    }

    public async Task<WorkshopSupplyProduct> UpdateSupplyProductAsync(
        string accountId, string productId, string? brand, string name, long expectedRevision,
        string idempotencyKey, CancellationToken ct = default)
    {
        RequireText(name, "Product name", 160);
        RequireText(idempotencyKey, "Idempotency key", 80);
        idempotencyKey = idempotencyKey.Trim();
        var product = await repository.GetAsync<WorkshopSupplyProduct>(
            DbCollections.WorkshopSupplyProducts, accountId, productId, ct)
            ?? throw new WorkshopValidationException("Supply product was not found.");
        var normalizedBrand = Trim(brand);
        var normalizedName = name.Trim();
        var payload = JsonSerializer.Serialize(new
        {
            productId, brand = normalizedBrand, name = normalizedName, expectedRevision
        });
        var sources = new[] { productId };
        var existing = await repository.FindOperationAsync(accountId, idempotencyKey, ct);
        if (existing is not null)
        {
            if (existing.OperationType != WorkshopOperationType.SupplyProductUpdated ||
                existing.PayloadHash != PayloadHash(WorkshopOperationType.SupplyProductUpdated,
                    existing.Deltas, existing.SourceEntityIds, payload, null))
                throw new WorkshopConflictException("Idempotency key was already used for a different operation.");
            await CommitAsync(accountId, idempotencyKey, WorkshopOperationType.SupplyProductUpdated,
                existing.EffectiveAt, existing.Deltas, existing.SourceEntityIds, payload, ct);
            product = await repository.GetAsync<WorkshopSupplyProduct>(
                DbCollections.WorkshopSupplyProducts, accountId, productId, ct)
                ?? throw new WorkshopConflictException("Supply product could not be recovered.");
            if (product.Revision > expectedRevision)
                return product.Brand == normalizedBrand && product.Name == normalizedName
                    ? product
                    : throw new WorkshopConflictException("Supply product was changed again after this update.");
        }
        else if (product.Revision != expectedRevision)
            throw new WorkshopConflictException("Supply product changed. Reload before updating.");

        var updated = product with
        {
            Brand = normalizedBrand,
            Name = normalizedName,
            Revision = product.Revision + 1
        };
        if (existing is null)
            await CommitAsync(accountId, idempotencyKey, WorkshopOperationType.SupplyProductUpdated,
                Now(), [], sources, payload, ct);
        if (!await repository.ReplaceAsync(DbCollections.WorkshopSupplyProducts,
                updated, accountId, product.Revision, ct))
            throw new WorkshopConflictException("Supply product changed while updating it.");
        return updated;
    }

    public async Task<WorkshopSupplyProduct> SetSupplyProductArchivedAsync(
        string accountId, string productId, bool archived, long expectedRevision,
        string idempotencyKey, CancellationToken ct = default)
    {
        RequireText(idempotencyKey, "Idempotency key", 80);
        idempotencyKey = idempotencyKey.Trim();
        var product = await repository.GetAsync<WorkshopSupplyProduct>(
            DbCollections.WorkshopSupplyProducts, accountId, productId, ct)
            ?? throw new WorkshopValidationException("Supply product was not found.");
        var payload = JsonSerializer.Serialize(new { productId, archived, expectedRevision });
        var sources = new[] { productId };
        var operationType = archived
            ? WorkshopOperationType.SupplyProductArchived
            : WorkshopOperationType.SupplyProductUpdated;
        var existing = await repository.FindOperationAsync(accountId, idempotencyKey, ct);
        if (existing is not null)
        {
            if (existing.OperationType != operationType ||
                existing.PayloadHash != PayloadHash(operationType, existing.Deltas,
                    existing.SourceEntityIds, payload, null))
                throw new WorkshopConflictException("Idempotency key was already used for a different operation.");
            await CommitAsync(accountId, idempotencyKey, operationType,
                existing.EffectiveAt, existing.Deltas, existing.SourceEntityIds, payload, ct);
            product = await repository.GetAsync<WorkshopSupplyProduct>(
                DbCollections.WorkshopSupplyProducts, accountId, productId, ct)
                ?? throw new WorkshopConflictException("Supply product could not be recovered.");
            if (product.Archived == archived && product.Revision > expectedRevision) return product;
        }
        else if (product.Revision != expectedRevision)
            throw new WorkshopConflictException("Supply product changed. Reload before updating it.");

        if (product.Archived == archived) return product;
        var updated = product with { Archived = archived, Revision = product.Revision + 1 };
        if (existing is null)
            await CommitAsync(accountId, idempotencyKey, operationType,
                Now(), [], sources, payload, ct);
        if (!await repository.ReplaceAsync(DbCollections.WorkshopSupplyProducts,
                updated, accountId, product.Revision, ct))
            throw new WorkshopConflictException("Supply product changed while updating it.");
        return updated;
    }

    public async Task DeleteSupplyProductAsync(
        string accountId, string productId, long expectedRevision,
        string idempotencyKey, CancellationToken ct = default)
    {
        RequireText(idempotencyKey, "Idempotency key", 80);
        idempotencyKey = idempotencyKey.Trim();
        var payload = JsonSerializer.Serialize(new { productId, expectedRevision });
        var sources = new[] { productId };
        var existing = await repository.FindOperationAsync(accountId, idempotencyKey, ct);
        if (existing is not null &&
            (existing.OperationType != WorkshopOperationType.SupplyProductDeleted ||
             existing.PayloadHash != PayloadHash(WorkshopOperationType.SupplyProductDeleted,
                 existing.Deltas, existing.SourceEntityIds, payload, null)))
            throw new WorkshopConflictException("Idempotency key was already used for a different operation.");

        var product = await repository.GetAsync<WorkshopSupplyProduct>(
            DbCollections.WorkshopSupplyProducts, accountId, productId, ct);
        if (product is null)
        {
            if (existing is not null) return;
            throw new WorkshopValidationException("Supply product was not found.");
        }
        if (product.Revision != expectedRevision)
            throw new WorkshopConflictException("Supply product changed. Reload before deleting it.");
        if (await HasSupplyProductReferencesAsync(accountId, productId, ct))
            throw new WorkshopConflictException("This product has purchase or recipe history. Archive it instead.");

        if (existing is null)
            await CommitAsync(accountId, idempotencyKey, WorkshopOperationType.SupplyProductDeleted,
                Now(), [], sources, payload, ct);
        if (!await repository.DeleteAsync<WorkshopSupplyProduct>(
                DbCollections.WorkshopSupplyProducts, accountId, productId, expectedRevision, ct))
            throw new WorkshopConflictException("Supply product changed while deleting it.");
    }

    private async Task<bool> HasSupplyProductReferencesAsync(
        string accountId, string productId, CancellationToken ct)
    {
        var purchases = await repository.ListAsync<WorkshopSupplyPurchase>(
            DbCollections.WorkshopSupplyPurchases, accountId, ct);
        if (purchases.Any(x => x.ProductId == productId)) return true;
        var lots = await repository.ListAsync<WorkshopSupplyLot>(DbCollections.WorkshopSupplyLots, accountId, ct);
        if (lots.Any(x => x.ProductId == productId)) return true;
        var recipes = await repository.ListAsync<AmmoRecipe>(DbCollections.WorkshopRecipes, accountId, ct);
        if (recipes.Any(x => x.PrimerProductId == productId || x.PowderProductId == productId)) return true;
        var recipeVersions = await repository.ListAsync<AmmoRecipeVersion>(
            DbCollections.WorkshopRecipeVersions, accountId, ct);
        if (recipeVersions.Any(x =>
                x.Snapshot.PrimerProductId == productId || x.Snapshot.PowderProductId == productId))
            return true;
        var sessions = await repository.ListAsync<ProductionSession>(
            DbCollections.WorkshopProductionSessions, accountId, ct);
        if (sessions.Any(x => x.BulletProductId == productId ||
                x.RecipeSnapshot.PrimerProductId == productId ||
                x.RecipeSnapshot.PowderProductId == productId))
            return true;
        var batches = await repository.ListAsync<AmmoBatch>(DbCollections.WorkshopAmmoBatches, accountId, ct);
        if (batches.Any(x => x.BulletProductId == productId ||
                x.RecipeSnapshot.PrimerProductId == productId ||
                x.RecipeSnapshot.PowderProductId == productId))
            return true;
        var operations = await repository.ListAsync<WorkshopOperation>(DbCollections.WorkshopOperations, accountId, ct);
        return operations.Any(x =>
            x.OperationType is not (WorkshopOperationType.SupplyProductCreated or
                WorkshopOperationType.SupplyProductUpdated or WorkshopOperationType.SupplyProductArchived or
                WorkshopOperationType.SupplyProductDeleted) &&
            (x.SourceEntityIds.Contains(productId, StringComparer.Ordinal) ||
             x.InventoryFacts.Any(fact => fact.ProductId == productId) ||
             x.ProductionSnapshot is { } snapshot &&
                 (snapshot.PrimerProductId == productId || snapshot.PowderProductId == productId) ||
             x.ProductionCorrection is { } correction &&
                 (correction.PreviousBulletProductId == productId || correction.BulletProductId == productId)));
    }

    public async Task<(WorkshopSupplyPurchase Purchase, IReadOnlyList<WorkshopSupplyLot> Lots)> RecordSupplyPurchaseAsync(
        string accountId, string productId, DateTime purchaseDate, long quantity, decimal totalPrice,
        string currency, string idempotencyKey, CancellationToken ct = default) =>
        await RecordSupplyPurchaseAsync(accountId, productId, purchaseDate, quantity, null, null,
            totalPrice, currency, idempotencyKey, ct);

    public async Task<(WorkshopSupplyPurchase Purchase, IReadOnlyList<WorkshopSupplyLot> Lots)> RecordSupplyPurchaseAsync(
        string accountId, string productId, DateTime purchaseDate, long? quantity,
        long? packageCount, long? unitsPerPackage, decimal totalPrice, string currency,
        string idempotencyKey, CancellationToken ct = default)
    {
        ValidateMoney(totalPrice, currency);
        RequireText(idempotencyKey, "Idempotency key", 80);
        idempotencyKey = idempotencyKey.Trim();
        if (purchaseDate == default) throw new WorkshopValidationException("Purchase date is required.");
        var product = await repository.GetAsync<WorkshopSupplyProduct>(DbCollections.WorkshopSupplyProducts, accountId, productId, ct)
            ?? throw new WorkshopValidationException("Supply product was not found.");
        if (product.Archived) throw new WorkshopValidationException("Archived products cannot be purchased.");
        var packageAware = packageCount.HasValue || unitsPerPackage.HasValue;
        long purchasedQuantity;
        long purchaseQuantity;
        if (packageAware)
        {
            if (product.Type is not (WorkshopSupplyType.Primer or WorkshopSupplyType.Bullet))
                throw new WorkshopValidationException("Package sizes are only supported for primer and bullet purchases.");
            if (quantity.HasValue || !packageCount.HasValue || !unitsPerPackage.HasValue ||
                packageCount is < 1 or > MaximumPackageCount || unitsPerPackage is < 1)
                throw new WorkshopValidationException("Enter a package count and positive units per package.");
            try
            {
                purchasedQuantity = checked(packageCount.Value * unitsPerPackage.Value);
            }
            catch (OverflowException)
            {
                throw new WorkshopValidationException("The total number of purchased units is too large.");
            }
            if (purchasedQuantity > MaximumSafeJavaScriptInteger)
                throw new WorkshopValidationException("The total number of purchased units is too large.");
            purchaseQuantity = packageCount.Value;
        }
        else
        {
            if (quantity is < 1 or > MaximumPackageCount)
                throw new WorkshopValidationException("Purchase quantity must be between 1 and 10000.");
            purchaseQuantity = quantity.Value;
            purchasedQuantity = quantity.Value;
        }

        var purchaseId = StableId(accountId, idempotencyKey, "supply-purchase");
        var packageLots = product.Type is WorkshopSupplyType.Powder or WorkshopSupplyType.CommercialAmmunition;
        var lotCount = packageLots ? checked((int)purchaseQuantity) : 1;
        var lotIds = Enumerable.Range(0, lotCount)
            .Select(index => StableId(accountId, idempotencyKey, $"supply-lot-{index}")).ToArray();
        var purchase = new WorkshopSupplyPurchase
        {
            Id = purchaseId, AccountId = accountId, ProductId = productId, PurchasedAt = purchaseDate,
            Quantity = purchasedQuantity, TotalPrice = totalPrice, Currency = currency, LotIds = lotIds,
            PackageCount = packageAware ? packageCount : null,
            UnitsPerPackage = packageAware ? unitsPerPackage : null,
            CreationDateTime = Now()
        };
        var perPackage = packageLots
            ? decimal.Round(totalPrice / purchaseQuantity, 2, MidpointRounding.AwayFromZero)
            : totalPrice;
        var lots = lotIds.Select((lotId, index) =>
        {
            var lotPrice = packageLots && index == lotIds.Length - 1
                ? totalPrice - perPackage * (purchaseQuantity - 1)
                : perPackage;
            var lotQuantity = packageLots ? 1 : purchasedQuantity;
            return new WorkshopSupplyLot
            {
                Id = lotId, AccountId = accountId, PurchaseId = purchaseId, ProductId = productId,
                PurchasedAt = purchaseDate, PurchasedQuantity = lotQuantity, AvailableQuantity = 0,
                TotalPrice = lotPrice, Currency = currency, Status = WorkshopSupplyLotStatus.Available,
                ItemCount = product.Type == WorkshopSupplyType.Case ? lotQuantity : null,
                PackageCount = packageAware ? packageCount : null,
                UnitsPerPackage = packageAware ? unitsPerPackage : null,
                CreationDateTime = Now()
            };
        }).ToArray();
        var payload = packageAware
            ? JsonSerializer.Serialize(new { productId, purchaseDate, packageCount, unitsPerPackage, totalPrice, currency })
            : JsonSerializer.Serialize(new { productId, purchaseDate, quantity = purchaseQuantity, totalPrice, currency });
        var deltas = lots.Select(x => new WorkshopDelta(
            WorkshopAggregateKind.SupplyLot, x.Id, StockDelta: x.PurchasedQuantity)).ToArray();
        var sources = new[] { purchaseId }.Concat(lotIds).Append(productId).ToArray();
        var existing = await repository.FindOperationAsync(accountId, idempotencyKey, ct);
        if (existing is not null &&
            existing.PayloadHash != PayloadHash(WorkshopOperationType.SupplyPurchaseRecorded, deltas, sources, payload, null))
            throw new WorkshopConflictException("Idempotency key was already used for a different operation.");

        if (await repository.GetAsync<WorkshopSupplyPurchase>(DbCollections.WorkshopSupplyPurchases, accountId, purchaseId, ct) is null)
            await repository.AddAsync(DbCollections.WorkshopSupplyPurchases, purchase, ct);
        foreach (var lot in lots)
            if (await repository.GetAsync<WorkshopSupplyLot>(DbCollections.WorkshopSupplyLots, accountId, lot.Id, ct) is null)
                await repository.AddAsync(DbCollections.WorkshopSupplyLots, lot, ct);

        await CommitAsync(accountId, idempotencyKey.Trim(), WorkshopOperationType.SupplyPurchaseRecorded,
            purchaseDate, deltas, sources, payload, ct,
            inventoryFacts: [new("purchase", productId, purchaseId, Currency: currency, CurrentAmount: totalPrice)]);
        var savedLots = new List<WorkshopSupplyLot>(lots.Length);
        foreach (var lot in lots)
            savedLots.Add(await repository.GetAsync<WorkshopSupplyLot>(DbCollections.WorkshopSupplyLots, accountId, lot.Id, ct) ?? lot);
        return (purchase, savedLots);
    }

    public async Task<WorkshopSupplyLot> AdjustSupplyStockAsync(
        string accountId, string lotId, long physicalCount, long expectedRevision,
        string reason, string idempotencyKey, CancellationToken ct = default)
    {
        RequireText(reason, "Adjustment reason", 500);
        RequireText(idempotencyKey, "Idempotency key", 80);
        idempotencyKey = idempotencyKey.Trim();
        var lot = await repository.GetAsync<WorkshopSupplyLot>(DbCollections.WorkshopSupplyLots, accountId, lotId, ct)
            ?? throw new WorkshopValidationException("Supply lot was not found.");
        var product = await repository.GetAsync<WorkshopSupplyProduct>(DbCollections.WorkshopSupplyProducts, accountId, lot.ProductId, ct)
            ?? throw new WorkshopValidationException("Supply product was not found.");
        if (product.Type is not (WorkshopSupplyType.Primer or WorkshopSupplyType.Bullet or WorkshopSupplyType.Case))
            throw new WorkshopValidationException("Physical count adjustments are only available for primers, bullets, and cases.");
        var command = new { lotId, physicalCount, expectedRevision, reason = reason.Trim() };
        var payload = JsonSerializer.Serialize(command);
        var delta = new WorkshopDelta(WorkshopAggregateKind.SupplyLot, lotId,
            StockDelta: physicalCount - lot.AvailableQuantity);
        var deltas = delta.StockDelta == 0 ? [] : new[] { delta };
        var sources = new[] { lotId, product.Id };
        var existing = await repository.FindOperationAsync(accountId, idempotencyKey, ct);
        if (existing is not null)
        {
            if (existing.PayloadHash != PayloadHash(WorkshopOperationType.SupplyStockAdjusted,
                    existing.Deltas, existing.SourceEntityIds, payload, null))
                throw new WorkshopConflictException("Idempotency key was already used for a different operation.");
            await CommitAsync(accountId, idempotencyKey, WorkshopOperationType.SupplyStockAdjusted,
                Now(), existing.Deltas, existing.SourceEntityIds, payload, ct);
        }
        else
        {
            if (lot.Revision != expectedRevision) throw new WorkshopConflictException("Supply lot changed. Reload before adjusting stock.");
            await CommitAsync(accountId, idempotencyKey, WorkshopOperationType.SupplyStockAdjusted,
                Now(), deltas, sources, payload, ct,
                inventoryFacts: [new("adjustment", product.Id, LotId: lotId, PreviousAmount: lot.AvailableQuantity,
                    CurrentAmount: physicalCount, Currency: lot.Currency, Reason: reason.Trim())]);
        }

        var projected = await repository.GetAsync<WorkshopSupplyLot>(DbCollections.WorkshopSupplyLots, accountId, lotId, ct)
            ?? throw new WorkshopConflictException("Supply lot projection could not be recovered.");
        if (product.Type == WorkshopSupplyType.Case && projected.ItemCount != physicalCount)
        {
            var updated = projected with { ItemCount = physicalCount, Revision = projected.Revision + 1 };
            if (!await repository.ReplaceAsync(DbCollections.WorkshopSupplyLots, updated, accountId, projected.Revision, ct))
                throw new WorkshopConflictException("Supply lot changed while saving the physical count.");
            projected = updated;
        }
        return projected;
    }

    public async Task<WorkshopSupplyLot> SetSupplyLotStatusAsync(
        string accountId, string lotId, bool isEmpty, long expectedRevision, string idempotencyKey,
        CancellationToken ct = default)
    {
        RequireText(idempotencyKey, "Idempotency key", 80);
        idempotencyKey = idempotencyKey.Trim();
        var lot = await repository.GetAsync<WorkshopSupplyLot>(DbCollections.WorkshopSupplyLots, accountId, lotId, ct)
            ?? throw new WorkshopValidationException("Supply lot was not found.");
        var product = await repository.GetAsync<WorkshopSupplyProduct>(DbCollections.WorkshopSupplyProducts, accountId, lot.ProductId, ct)
            ?? throw new WorkshopValidationException("Supply product was not found.");
        if (product.Type is not (WorkshopSupplyType.Powder or WorkshopSupplyType.Case or WorkshopSupplyType.CommercialAmmunition))
            throw new WorkshopValidationException("This supply type does not use a package status.");
        var payload = JsonSerializer.Serialize(new { lotId, isEmpty, expectedRevision });
        var existing = await repository.FindOperationAsync(accountId, idempotencyKey, ct);
        IReadOnlyList<WorkshopDelta> deltas;
        IReadOnlyList<string> sources;
        if (existing is not null)
        {
            if (existing.PayloadHash != PayloadHash(WorkshopOperationType.SupplyLotStatusChanged,
                    existing.Deltas, existing.SourceEntityIds, payload, null))
                throw new WorkshopConflictException("Idempotency key was already used for a different operation.");
            deltas = existing.Deltas;
            sources = existing.SourceEntityIds;
        }
        else
        {
            if (lot.Revision != expectedRevision) throw new WorkshopConflictException("Supply lot changed. Reload before changing status.");
            var target = isEmpty ? 0 : (product.Type == WorkshopSupplyType.Case
                ? lot.ItemCount ?? lot.PurchasedQuantity : 1);
            var delta = target - lot.AvailableQuantity;
            deltas = delta == 0 ? [] : [new(WorkshopAggregateKind.SupplyLot, lotId, StockDelta: delta)];
            sources = [lotId, product.Id];
        }
        await CommitAsync(accountId, idempotencyKey, WorkshopOperationType.SupplyLotStatusChanged,
            Now(), deltas, sources, payload, ct,
            inventoryFacts: [new("status", product.Id, LotId: lotId, CurrentAmount: isEmpty ? 1 : 0)]);
        var projected = await repository.GetAsync<WorkshopSupplyLot>(DbCollections.WorkshopSupplyLots, accountId, lotId, ct)
            ?? throw new WorkshopConflictException("Supply lot projection could not be recovered.");
        var desiredStatus = isEmpty ? WorkshopSupplyLotStatus.Empty : WorkshopSupplyLotStatus.Available;
        if (projected.Status != desiredStatus)
        {
            var updated = projected with { Status = desiredStatus, Revision = projected.Revision + 1 };
            if (!await repository.ReplaceAsync(DbCollections.WorkshopSupplyLots, updated, accountId, projected.Revision, ct))
                throw new WorkshopConflictException("Supply lot changed while saving its status.");
            projected = updated;
        }
        return projected;
    }

    public async Task<WorkshopComponentPurchase> RecordComponentPurchaseAsync(
        string accountId, string componentId, DateTime purchaseDate, decimal totalPrice,
        string currency, string idempotencyKey, CancellationToken ct = default)
    {
        _ = await repository.GetAsync<ComponentInstance>(DbCollections.WorkshopComponentInstances, accountId, componentId, ct)
            ?? throw new WorkshopValidationException("Component was not found.");
        ValidateMoney(totalPrice, currency);
        RequireText(idempotencyKey, "Idempotency key", 80);
        if (purchaseDate == default) throw new WorkshopValidationException("Purchase date is required.");
        idempotencyKey = idempotencyKey.Trim();
        var purchaseId = StableId(accountId, componentId, "component-purchase");
        var payload = JsonSerializer.Serialize(new { componentId, purchaseDate, totalPrice, currency });
        var existing = await repository.FindOperationAsync(accountId, idempotencyKey, ct);
        if (existing is not null)
        {
            var expected = PayloadHash(WorkshopOperationType.ComponentPurchaseRecorded, existing.Deltas,
                existing.SourceEntityIds, payload, null);
            if (existing.OperationType != WorkshopOperationType.ComponentPurchaseRecorded || existing.PayloadHash != expected)
                throw new WorkshopConflictException("Idempotency key was already used for a different operation.");
            await CommitAsync(accountId, idempotencyKey, existing.OperationType, existing.EffectiveAt,
                existing.Deltas, existing.SourceEntityIds, payload, ct, inventoryFacts: existing.InventoryFacts);
            var recovered = await repository.GetAsync<WorkshopComponentPurchase>(
                DbCollections.WorkshopComponentPurchases, accountId, purchaseId, ct);
            if (recovered is not null) return recovered;
            var recoveredFact = NewComponentPurchase(accountId, componentId, purchaseId, purchaseDate, totalPrice, currency, 1);
            await repository.AddAsync(DbCollections.WorkshopComponentPurchases, recoveredFact, ct);
            return recoveredFact;
        }
        if (await repository.GetAsync<WorkshopComponentPurchase>(
                DbCollections.WorkshopComponentPurchases, accountId, purchaseId, ct) is not null)
            throw new WorkshopConflictException("A component purchase already exists. Use PUT to correct it.");
        var purchase = NewComponentPurchase(accountId, componentId, purchaseId, purchaseDate, totalPrice, currency, 1);
        var sources = new[] { componentId, purchaseId };
        await CommitAsync(accountId, idempotencyKey, WorkshopOperationType.ComponentPurchaseRecorded,
            purchaseDate, [], sources, payload, ct,
            inventoryFacts: [new("component-purchase", ComponentId: componentId,
                CurrentAmount: totalPrice,
                Currency: currency)]);
        await repository.AddAsync(DbCollections.WorkshopComponentPurchases, purchase, ct);
        return purchase;
    }

    public async Task<WorkshopComponentPurchase> CorrectComponentPurchaseAsync(
        string accountId, string componentId, DateTime purchaseDate, decimal totalPrice, string currency,
        long expectedRevision, string? reason, string idempotencyKey, CancellationToken ct = default)
    {
        _ = await repository.GetAsync<ComponentInstance>(
            DbCollections.WorkshopComponentInstances, accountId, componentId, ct)
            ?? throw new WorkshopValidationException("Component was not found.");
        ValidateMoney(totalPrice, currency);
        var normalizedReason = Trim(reason);
        if (normalizedReason?.Length > 500)
            throw new WorkshopValidationException("Correction reason must be at most 500 characters.");
        RequireText(idempotencyKey, "Idempotency key", 80);
        if (purchaseDate == default) throw new WorkshopValidationException("Purchase date is required.");
        idempotencyKey = idempotencyKey.Trim();
        var purchaseId = StableId(accountId, componentId, "component-purchase");
        var purchase = await repository.GetAsync<WorkshopComponentPurchase>(
            DbCollections.WorkshopComponentPurchases, accountId, purchaseId, ct)
            ?? throw new WorkshopValidationException("Component purchase information was not found.");
        var payload = JsonSerializer.Serialize(new
        {
            componentId, purchaseDate, totalPrice, currency, expectedRevision, reason = normalizedReason
        });
        var existing = await repository.FindOperationAsync(accountId, idempotencyKey, ct);
        if (existing is not null)
        {
            var expected = PayloadHash(WorkshopOperationType.ComponentPurchaseCorrected, existing.Deltas,
                existing.SourceEntityIds, payload, null);
            if (existing.OperationType != WorkshopOperationType.ComponentPurchaseCorrected ||
                existing.PayloadHash != expected)
                throw new WorkshopConflictException("Idempotency key was already used for a different operation.");
            await CommitAsync(accountId, idempotencyKey, WorkshopOperationType.ComponentPurchaseCorrected,
                existing.EffectiveAt, existing.Deltas, existing.SourceEntityIds, payload, ct,
                inventoryFacts: existing.InventoryFacts);
            purchase = await repository.GetAsync<WorkshopComponentPurchase>(
                DbCollections.WorkshopComponentPurchases, accountId, purchaseId, ct) ?? purchase;
            if (purchase.Revision >= expectedRevision + 1) return purchase;
        }
        else if (purchase.Revision != expectedRevision)
            throw new WorkshopConflictException("Component purchase changed. Reload before correcting it.");

        var sources = new[] { componentId, purchaseId };
        var replacement = purchase with
        {
            PurchaseDate = purchaseDate, TotalPrice = totalPrice, Currency = currency,
            Revision = purchase.Revision + 1,
            History = purchase.History.Append(new WorkshopComponentPurchaseRevision(
                purchase.Revision + 1, purchaseDate, totalPrice, currency, normalizedReason, Now())).ToArray()
        };
        if (existing is null)
            await CommitAsync(accountId, idempotencyKey, WorkshopOperationType.ComponentPurchaseCorrected,
                Now(), [], sources, payload, ct,
                inventoryFacts: [new("component-purchase-correction", ComponentId: componentId,
                    PreviousAmount: purchase.TotalPrice, CurrentAmount: totalPrice, Currency: currency,
                    Reason: normalizedReason)]);
        if (!await repository.ReplaceAsync(DbCollections.WorkshopComponentPurchases,
                replacement, accountId, expectedRevision, ct))
            throw new WorkshopConflictException("Component purchase changed while saving the correction.");
        return replacement;
    }

    private WorkshopComponentPurchase NewComponentPurchase(
        string accountId, string componentId, string id, DateTime purchaseDate,
        decimal totalPrice, string currency, long revision) =>
        new()
        {
            Id = id, AccountId = accountId, ComponentId = componentId, PurchaseDate = purchaseDate,
            TotalPrice = totalPrice, Currency = currency, Revision = revision,
            History = [new(revision, purchaseDate, totalPrice, currency, null, Now())],
            CreationDateTime = Now()
        };

    public async Task<WorkshopProductionMutationResult> RecordProductionWithSuppliesAsync(
        string accountId, WorkshopProductionCommand command, CancellationToken ct = default)
    {
        if (command.Quantity <= 0) throw new WorkshopValidationException("Produced quantity must be positive.");
        RequireText(command.IdempotencyKey, "Idempotency key", 80);
        command = command with { IdempotencyKey = command.IdempotencyKey.Trim() };
        var batchId = StableId(accountId, command.IdempotencyKey, "batch");
        var sessionId = StableId(accountId, command.IdempotencyKey, "production-session");
        var existing = await repository.FindOperationAsync(accountId, command.IdempotencyKey, ct);
        if (existing is not null)
        {
            var expected = PayloadHash(WorkshopOperationType.ProductionRecorded, existing.Deltas,
                existing.SourceEntityIds, JsonSerializer.Serialize(command), null);
            if (existing.PayloadHash != expected)
                throw new WorkshopConflictException("Idempotency key was already used for a different operation.");
            await CommitAsync(accountId, command.IdempotencyKey, WorkshopOperationType.ProductionRecorded,
                existing.EffectiveAt, existing.Deltas, existing.SourceEntityIds, JsonSerializer.Serialize(command), ct,
                inventoryFacts: existing.InventoryFacts, productionSnapshot: existing.ProductionSnapshot);
            return await LoadProductionMutationAsync(accountId, existing, sessionId, batchId, true, ct);
        }

        var recipe = await repository.GetAsync<AmmoRecipe>(DbCollections.WorkshopRecipes, accountId, command.RecipeId, ct)
            ?? throw new WorkshopValidationException("Recipe was not found.");
        if (recipe.Status != WorkshopRecipeStatus.Active)
            throw new WorkshopValidationException("Recipe was not found.");
        if (recipe.Version != command.RecipeVersion)
            throw new WorkshopConflictException("Recipe changed. Reload before recording production.");
        var snapshot = Snapshot(recipe);
        await ValidateRecipeProductsAsync(accountId, snapshot, ct, requireActive: false);
        _ = await RequireProductTypeAsync(accountId, recipe.PrimerProductId!, WorkshopSupplyType.Primer, ct);
        var bulletProduct = await RequireProductTypeAsync(accountId, command.BulletProductId, WorkshopSupplyType.Bullet, ct);
        var primers = await ResolveProductionLotsAsync(accountId, recipe.PrimerProductId!, command.Quantity,
            command.PrimerLotAllocations, [], ct);
        var bullets = await ResolveProductionLotsAsync(accountId, bulletProduct.Id, command.Quantity,
            command.BulletLotAllocations, [], ct);
        var allocations = primers.Concat(bullets).ToArray();
        var deltas = new List<WorkshopDelta> { new(WorkshopAggregateKind.Batch, batchId, StockDelta: command.Quantity) };
        deltas.AddRange(allocations.Select(x => new WorkshopDelta(
            WorkshopAggregateKind.SupplyLot, x.LotId, StockDelta: -x.Quantity)));
        var sourceIds = new[] { batchId, sessionId }.Concat(allocations.Select(x => x.LotId))
            .Distinct(StringComparer.Ordinal).ToArray();
        var payload = JsonSerializer.Serialize(command);
        var facts = await ProductionInventoryFactsAsync(accountId, primers, bullets, ct);

        var batch = await repository.GetAsync<AmmoBatch>(DbCollections.WorkshopAmmoBatches, accountId, batchId, ct);
        if (batch is null)
        {
            batch = new AmmoBatch
            {
                Id = batchId, AccountId = accountId, RecipeId = recipe.Id, RecipeSnapshot = snapshot,
                ProductionSessionId = sessionId, ProductionDate = command.ProductionDate,
                ProducedQuantity = command.Quantity, OnHandQuantity = 0, Notes = Trim(command.Notes),
                BulletProductId = bulletProduct.Id, PrimerLotAllocations = primers, BulletLotAllocations = bullets,
                CreationDateTime = Now()
            };
            await repository.AddAsync(DbCollections.WorkshopAmmoBatches, batch, ct);
        }
        var session = await repository.GetAsync<ProductionSession>(DbCollections.WorkshopProductionSessions, accountId, sessionId, ct);
        if (session is null)
        {
            session = new ProductionSession
            {
                Id = sessionId, AccountId = accountId, RecipeId = recipe.Id, RecipeSnapshot = snapshot,
                ProductionDate = command.ProductionDate, QuantityProduced = command.Quantity, Notes = Trim(command.Notes),
                BatchId = batchId, BulletProductId = bulletProduct.Id, PrimerLotAllocations = primers,
                BulletLotAllocations = bullets, CreationDateTime = Now()
            };
            await repository.AddAsync(DbCollections.WorkshopProductionSessions, session, ct);
        }
        var operation = await CommitAsync(accountId, command.IdempotencyKey, WorkshopOperationType.ProductionRecorded,
            command.ProductionDate, deltas, sourceIds, payload, ct, inventoryFacts: facts, productionSnapshot: snapshot);
        return await LoadProductionMutationAsync(accountId, operation.Operation, sessionId, batchId,
            operation.Idempotent, ct);
    }

    public async Task<WorkshopProductionMutationResult> UpdateProductionAsync(
        string accountId, string sessionId, WorkshopProductionEditCommand edit, CancellationToken ct = default)
    {
        var command = edit.Production;
        RequireText(command.IdempotencyKey, "Idempotency key", 80);
        command = command with { IdempotencyKey = command.IdempotencyKey.Trim() };
        edit = edit with { Production = command };
        if (command.Quantity <= 0) throw new WorkshopValidationException("Produced quantity must be positive.");
        var session = await repository.GetAsync<ProductionSession>(
            DbCollections.WorkshopProductionSessions, accountId, sessionId, ct)
            ?? throw new WorkshopValidationException("Production record was not found.");
        if (command.RecipeId != session.RecipeId)
            throw new WorkshopValidationException("The recipe identity of a production record cannot be changed.");
        var batch = await repository.GetAsync<AmmoBatch>(DbCollections.WorkshopAmmoBatches, accountId, session.BatchId, ct)
            ?? throw new WorkshopConflictException("The production batch could not be recovered.");
        var existing = await repository.FindOperationAsync(accountId, command.IdempotencyKey, ct);
        if (existing is not null)
        {
            var expected = PayloadHash(WorkshopOperationType.ProductionEdited, existing.Deltas,
                existing.SourceEntityIds, JsonSerializer.Serialize(new { edit.ExpectedRevision, command }), null);
            if (existing.PayloadHash != expected)
                throw new WorkshopConflictException("Idempotency key was already used for a different operation.");
            await CommitAsync(accountId, command.IdempotencyKey, WorkshopOperationType.ProductionEdited,
                existing.EffectiveAt, existing.Deltas, existing.SourceEntityIds,
                JsonSerializer.Serialize(new { edit.ExpectedRevision, command }), ct,
                inventoryFacts: existing.InventoryFacts, productionSnapshot: existing.ProductionSnapshot,
                productionCorrection: existing.ProductionCorrection);
            return await ApplyEditedProductionRecordAsync(accountId, session, batch, command, existing,
                edit.ExpectedRevision, true, ct, existing.ProductionSnapshot);
        }
        if (session.Revision != edit.ExpectedRevision)
            throw new WorkshopConflictException("Production record changed. Reload before editing.");
        var alreadyConsumed = Math.Max(0, session.QuantityProduced - batch.OnHandQuantity);
        if (command.Quantity < alreadyConsumed)
            throw new WorkshopConflictException(
                $"Production quantity cannot be lower than {alreadyConsumed} units already consumed from this batch.");
        var recipe = await repository.GetAsync<AmmoRecipe>(
            DbCollections.WorkshopRecipes, accountId, session.RecipeId, ct);
        var hasSnapshottedSupplyReferences =
            !string.IsNullOrWhiteSpace(session.RecipeSnapshot.PrimerProductId) &&
            !string.IsNullOrWhiteSpace(session.RecipeSnapshot.PowderProductId);
        RecipeSnapshot snapshot;
        if (hasSnapshottedSupplyReferences)
        {
            if (session.RecipeSnapshot.Version != command.RecipeVersion)
                throw new WorkshopConflictException("Production recipe snapshot changed. Reload before editing.");
            snapshot = session.RecipeSnapshot;
        }
        else
        {
            if (recipe is null)
                throw new WorkshopValidationException("Update this recipe with primer and powder products before editing production.");
            if (recipe.Version != command.RecipeVersion)
                throw new WorkshopConflictException("Recipe changed. Reload before editing production.");
            snapshot = Snapshot(recipe);
        }
        await ValidateRecipeProductsAsync(accountId, snapshot, ct, requireActive: false);
        var primerProductId = snapshot.PrimerProductId
            ?? throw new WorkshopValidationException("The production recipe has no primer product.");
        _ = await RequireProductTypeAsync(
            accountId, primerProductId, WorkshopSupplyType.Primer, ct, requireActive: false);
        var bulletProduct = await RequireProductTypeAsync(
            accountId, command.BulletProductId, WorkshopSupplyType.Bullet, ct,
            requireActive: command.BulletProductId != session.BulletProductId);
        if (command.ProductionDate == default) throw new WorkshopValidationException("Production date is required.");
        var priorSelections = session.PrimerLotAllocations.Concat(session.BulletLotAllocations).ToArray();
        var primers = await ResolveProductionLotsAsync(accountId, primerProductId, command.Quantity,
            command.PrimerLotAllocations, priorSelections, ct);
        var bullets = await ResolveProductionLotsAsync(accountId, bulletProduct.Id, command.Quantity,
            command.BulletLotAllocations, priorSelections, ct);
        var replacementAllocations = primers.Concat(bullets).ToArray();
        var deltaByLot = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var allocation in priorSelections)
            deltaByLot[allocation.LotId] = deltaByLot.GetValueOrDefault(allocation.LotId) + allocation.Quantity;
        foreach (var allocation in replacementAllocations)
            deltaByLot[allocation.LotId] = deltaByLot.GetValueOrDefault(allocation.LotId) - allocation.Quantity;
        var deltas = deltaByLot.Where(x => x.Value != 0)
            .Select(x => new WorkshopDelta(WorkshopAggregateKind.SupplyLot, x.Key, StockDelta: x.Value)).ToList();
        deltas.Add(new WorkshopDelta(WorkshopAggregateKind.Batch, batch.Id,
            StockDelta: command.Quantity - session.QuantityProduced));
        var sourceIds = new[] { session.Id, batch.Id }.Concat(deltaByLot.Keys)
            .Distinct(StringComparer.Ordinal).ToArray();
        var payload = JsonSerializer.Serialize(new { edit.ExpectedRevision, command });
        var facts = (await ProductionInventoryFactsAsync(accountId, primers, bullets, ct))
            .Concat((await ProductionInventoryFactsAsync(accountId,
                    session.PrimerLotAllocations, session.BulletLotAllocations, ct))
                .Select(x => x with { Type = $"reversed-{x.Type}" })).ToArray();
        var correction = new WorkshopProductionCorrectionFact(
            edit.ExpectedRevision, session.ProductionDate, session.QuantityProduced, session.BulletProductId,
            session.Notes, session.PrimerLotAllocations, session.BulletLotAllocations,
            command.ProductionDate, command.Quantity, bulletProduct.Id, Trim(command.Notes), primers, bullets);
        var result = await CommitAsync(accountId, command.IdempotencyKey, WorkshopOperationType.ProductionEdited,
            command.ProductionDate, deltas, sourceIds, payload, ct, inventoryFacts: facts,
            productionSnapshot: snapshot, productionCorrection: correction);
        session = await repository.GetAsync<ProductionSession>(
            DbCollections.WorkshopProductionSessions, accountId, sessionId, ct) ?? session;
        batch = await repository.GetAsync<AmmoBatch>(
            DbCollections.WorkshopAmmoBatches, accountId, session.BatchId, ct) ?? batch;
        return await ApplyEditedProductionRecordAsync(accountId, session, batch, command,
            result.Operation, edit.ExpectedRevision, result.Idempotent, ct, snapshot, primers, bullets, bulletProduct.Id);
    }

    private async Task<WorkshopProductionMutationResult> ApplyEditedProductionRecordAsync(
        string accountId, ProductionSession session, AmmoBatch batch, WorkshopProductionCommand command,
        WorkshopOperation operation, long expectedRevision, bool idempotent, CancellationToken ct,
        RecipeSnapshot? snapshot = null, IReadOnlyList<WorkshopLotAllocation>? primers = null,
        IReadOnlyList<WorkshopLotAllocation>? bullets = null, string? bulletProductId = null)
    {
        var current = await repository.GetAsync<ProductionSession>(
            DbCollections.WorkshopProductionSessions, accountId, session.Id, ct) ?? session;
        if (current.Revision == expectedRevision)
        {
            var recipe = await repository.GetAsync<AmmoRecipe>(DbCollections.WorkshopRecipes, accountId, current.RecipeId, ct);
            snapshot ??= recipe is null ? current.RecipeSnapshot : Snapshot(recipe);
            primers ??= AllocationsFromFacts(operation.InventoryFacts, "primer");
            bullets ??= AllocationsFromFacts(operation.InventoryFacts, "bullet");
            bulletProductId ??= command.BulletProductId;
            var updated = current with
            {
                ProductionDate = command.ProductionDate, QuantityProduced = command.Quantity,
                Notes = Trim(command.Notes), RecipeSnapshot = snapshot,
                BulletProductId = bulletProductId, PrimerLotAllocations = primers,
                BulletLotAllocations = bullets, Revision = current.Revision + 1
            };
            if (!await repository.ReplaceAsync(DbCollections.WorkshopProductionSessions, updated,
                    accountId, expectedRevision, ct))
                throw new WorkshopConflictException("Production record changed while saving the edit.");
            current = updated;
        }
        var currentBatch = await repository.GetAsync<AmmoBatch>(DbCollections.WorkshopAmmoBatches, accountId, batch.Id, ct) ?? batch;
        if (current.Revision == expectedRevision + 1 &&
            (currentBatch.ProducedQuantity != command.Quantity || currentBatch.ProductionDate != command.ProductionDate ||
             currentBatch.Notes != Trim(command.Notes) || currentBatch.BulletProductId != command.BulletProductId))
        {
            var updatedBatch = currentBatch with
            {
                RecipeSnapshot = current.RecipeSnapshot, ProductionDate = command.ProductionDate,
                ProducedQuantity = command.Quantity, Notes = Trim(command.Notes),
                BulletProductId = current.BulletProductId, PrimerLotAllocations = current.PrimerLotAllocations,
                BulletLotAllocations = current.BulletLotAllocations, Revision = currentBatch.Revision + 1
            };
            if (!await repository.ReplaceAsync(DbCollections.WorkshopAmmoBatches, updatedBatch,
                    accountId, currentBatch.Revision, ct))
                throw new WorkshopConflictException("Production batch changed while saving the edit.");
            currentBatch = updatedBatch;
        }
        return new(operation, current, currentBatch, idempotent);
    }

    private async Task<WorkshopProductionMutationResult> LoadProductionMutationAsync(
        string accountId, WorkshopOperation operation, string sessionId, string batchId, bool idempotent,
        CancellationToken ct)
    {
        var session = await repository.GetAsync<ProductionSession>(
            DbCollections.WorkshopProductionSessions, accountId, sessionId, ct)
            ?? throw new WorkshopConflictException("Production session could not be recovered.");
        var batch = await repository.GetAsync<AmmoBatch>(
            DbCollections.WorkshopAmmoBatches, accountId, batchId, ct)
            ?? throw new WorkshopConflictException("Production batch could not be recovered.");
        return new(operation, session, batch, idempotent);
    }

    private async Task<IReadOnlyList<WorkshopLotAllocation>> ResolveProductionLotsAsync(
        string accountId, string productId, long quantity, IReadOnlyList<WorkshopProductionSelection>? selected,
        IReadOnlyList<WorkshopLotAllocation> restore, CancellationToken ct)
    {
        var lots = await repository.ListAsync<WorkshopSupplyLot>(DbCollections.WorkshopSupplyLots, accountId, ct);
        var oldByLot = restore.GroupBy(x => x.LotId, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.Sum(y => y.Quantity), StringComparer.Ordinal);
        var available = lots.Where(x => x.ProductId == productId)
            .Select(x => new { Lot = x, Count = x.AvailableQuantity + oldByLot.GetValueOrDefault(x.Id) })
            .Where(x => x.Count > 0)
            .OrderBy(x => x.Lot.PurchasedAt).ThenBy(x => x.Lot.Id, StringComparer.Ordinal).ToArray();
        var expected = new List<(WorkshopSupplyLot Lot, long Quantity)>();
        var remaining = quantity;
        foreach (var item in available)
        {
            if (remaining == 0) break;
            var amount = Math.Min(remaining, item.Count);
            expected.Add((item.Lot, amount));
            remaining -= amount;
        }
        if (remaining > 0)
            throw new WorkshopValidationException("Insufficient supply stock for production.");
        var requested = selected ?? [];
        if (requested.Count != expected.Count || requested.Where((x, index) =>
                x.LotId != expected[index].Lot.Id || x.Quantity != expected[index].Quantity).Any())
            throw new WorkshopConflictException("Supply stock or FIFO order changed. Reload inventory and reselect lots.");
        return expected.Select(x => new WorkshopLotAllocation(x.Lot.Id, x.Quantity, x.Lot.Revision)).ToArray();
    }

    private async Task<WorkshopSupplyProduct> RequireProductTypeAsync(
        string accountId, string id, WorkshopSupplyType type, CancellationToken ct, bool requireActive = true)
    {
        var product = await repository.GetAsync<WorkshopSupplyProduct>(DbCollections.WorkshopSupplyProducts, accountId, id, ct)
            ?? throw new WorkshopValidationException("Supply product was not found.");
        if (requireActive && product.Archived || product.Type != type)
            throw new WorkshopValidationException($"Select an active {type} product.");
        return product;
    }

    private async Task ValidateRecipeProductsAsync(
        string accountId, RecipeSnapshot snapshot, CancellationToken ct, bool requireActive = true)
    {
        if (string.IsNullOrWhiteSpace(snapshot.PrimerProductId) || string.IsNullOrWhiteSpace(snapshot.PowderProductId))
            throw new WorkshopValidationException("Select both a primer and powder product before saving this recipe.");
        _ = await RequireProductTypeAsync(accountId, snapshot.PrimerProductId, WorkshopSupplyType.Primer, ct, requireActive);
        _ = await RequireProductTypeAsync(accountId, snapshot.PowderProductId, WorkshopSupplyType.Powder, ct, requireActive);
    }

    private static RecipeSnapshot Snapshot(AmmoRecipe recipe) =>
        new(recipe.Name, recipe.Caliber, recipe.Oal, recipe.BulletWeight, recipe.BulletForm, recipe.Primer,
            recipe.Powder, recipe.PowderWeight, recipe.Notes, recipe.PrimerProductId, recipe.PowderProductId, recipe.Version);

    private async Task<IReadOnlyList<WorkshopInventoryFact>> ProductionInventoryFactsAsync(
        string accountId, IReadOnlyList<WorkshopLotAllocation> primers,
        IReadOnlyList<WorkshopLotAllocation> bullets, CancellationToken ct)
    {
        async Task<WorkshopInventoryFact> CreateFact(WorkshopLotAllocation allocation, string kind)
        {
            var lot = await repository.GetAsync<WorkshopSupplyLot>(
                DbCollections.WorkshopSupplyLots, accountId, allocation.LotId, ct)
                ?? throw new WorkshopValidationException("Selected supply lot was not found.");
            return new(kind, lot.ProductId, lot.PurchaseId, lot.Id,
                PreviousAmount: lot.TotalPrice, CurrentAmount: allocation.Quantity,
                Currency: lot.Currency, Reason: $"quantity:{lot.PurchasedQuantity}");
        }

        var result = new List<WorkshopInventoryFact>(primers.Count + bullets.Count);
        foreach (var allocation in primers) result.Add(await CreateFact(allocation, "primer-consumption"));
        foreach (var allocation in bullets) result.Add(await CreateFact(allocation, "bullet-consumption"));
        return result;
    }

    private static IReadOnlyList<WorkshopLotAllocation> AllocationsFromFacts(
        IReadOnlyList<WorkshopInventoryFact> facts, string type) =>
        facts.Where(x => x.Type == $"{type}-consumption" && x.LotId is not null && x.CurrentAmount.HasValue)
            .Select(x => new WorkshopLotAllocation(x.LotId!, decimal.ToInt64(x.CurrentAmount!.Value), 0)).ToArray();

    private static void ValidateMoney(decimal amount, string currency)
    {
        if (amount < 0 || amount > 999999999999.99m || decimal.Round(amount, 2) != amount)
            throw new WorkshopValidationException("Price must be nonnegative and have at most two decimal places.");
        if (currency.Length != 3 || currency.Any(x => x is < 'A' or > 'Z'))
            throw new WorkshopValidationException("Currency must be a three-letter uppercase ISO code.");
    }

    private static void AddCurrency(IDictionary<string, decimal> totals, string currency, decimal amount)
    {
        totals.TryGetValue(currency, out var current);
        totals[currency] = current + amount;
    }

    private static IReadOnlyList<(string Currency, decimal Total)> CurrencyTotals(
        IReadOnlyDictionary<string, decimal> totals) =>
        totals.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => (x.Key, x.Value)).ToArray();
}
