using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MongoDB.Bson;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models.Workshop;

namespace MorWalPizVideo.Domain.Workshop;

public sealed record WorkshopFirearmCommand(string Manufacturer, string Model, string Caliber, string? Category,
    string? Nickname, DateTime? PurchaseDate, string? Notes);
public sealed record WorkshopUpdateFirearmCommand(string Manufacturer, string Model, string Caliber, string? Category,
    string? Nickname, DateTime? PurchaseDate, string? Notes, long ExpectedRevision, string IdempotencyKey);
public sealed record WorkshopArchiveFirearmCommand(long ExpectedRevision, string IdempotencyKey);
public sealed record WorkshopComponentDefinitionCommand(string Name, string? Manufacturer, string? Model,
    string? Description, long? DefaultServiceInterval, WorkshopTrackingMode TrackingMode, string? Notes);
public sealed record WorkshopComponentCommand(string DefinitionId, string? IdentificationNote, string? Manufacturer,
    string? Model, long? ServiceInterval, WorkshopTrackingMode? TrackingMode, string? Notes);
public sealed record WorkshopInstallCommand(string ComponentId, string FirearmId, DateTime InstalledAt,
    string? InstallationNotes, string IdempotencyKey);
public sealed record WorkshopRemoveCommand(string InstallationId, DateTime RemovedAt,
    string? RemovalNotes, long ExpectedRevision, string IdempotencyKey);
public sealed record WorkshopMaintenanceCommand(string? FirearmId, string? ComponentId,
    WorkshopMaintenanceEventType EventType, DateTime Date, long? RoundCountAtEvent,
    string Description, string? Notes, string IdempotencyKey);
public sealed record WorkshopShootingCommand(string FirearmId, DateTime Date, long TotalRounds, string? Notes,
    IReadOnlyList<ShootingConsumption> ConsumptionRecords, long UntrackedAmmunition,
    IReadOnlyList<ShootingUsageSegment> UsageSegments, string IdempotencyKey);
public sealed record WorkshopProductionCommand(string RecipeId, DateTime ProductionDate, long Quantity, string? Notes, string IdempotencyKey);
public sealed record WorkshopAllocationCommand(string BatchId, string FirearmId, long Quantity, string? Notes, string IdempotencyKey);
public sealed record WorkshopReleaseCommand(string AllocationId, long Quantity, long ExpectedRevision, string IdempotencyKey);
public sealed record WorkshopReallocateCommand(string SourceAllocationId, string DestinationFirearmId,
    long Quantity, string IdempotencyKey);
public sealed record WorkshopCorrectionCommand(string OriginalOperationId, WorkshopShootingCommand? Replacement,
    string IdempotencyKey);
public sealed record WorkshopDashboardData(int FirearmCount, int ComponentCount, long TotalFirearmRounds,
    int ComponentsAtOrAboveInterval, long TotalProducedRounds, long AvailableRounds, long AllocatedRounds,
    int RecentActivityCount, WorkshopProjectionState ProjectionState);
public sealed record WorkshopOperationResult(WorkshopOperation Operation, bool Idempotent);

public sealed class WorkshopValidationException(string message) : Exception(message);
public sealed class WorkshopConflictException(string message) : Exception(message);

public sealed class WorkshopService(
    IWorkshopRepository repository,
    WorkshopReconciliationService reconciliation,
    TimeProvider timeProvider)
{
    public Task<IReadOnlyList<Firearm>> GetFirearmsAsync(string accountId, CancellationToken ct = default) =>
        repository.ListAsync<Firearm>(DbCollections.WorkshopFirearms, accountId, ct);

    public Task<IReadOnlyList<ComponentDefinition>> GetComponentDefinitionsAsync(string accountId, CancellationToken ct = default) =>
        repository.ListAsync<ComponentDefinition>(DbCollections.WorkshopComponentDefinitions, accountId, ct);

    public Task<IReadOnlyList<ComponentInstance>> GetComponentsAsync(string accountId, CancellationToken ct = default) =>
        repository.ListAsync<ComponentInstance>(DbCollections.WorkshopComponentInstances, accountId, ct);

    public Task<IReadOnlyList<AmmoRecipe>> GetRecipesAsync(string accountId, CancellationToken ct = default) =>
        repository.ListAsync<AmmoRecipe>(DbCollections.WorkshopRecipes, accountId, ct);

    public Task<IReadOnlyList<AmmoBatch>> GetBatchesAsync(string accountId, CancellationToken ct = default) =>
        repository.ListAsync<AmmoBatch>(DbCollections.WorkshopAmmoBatches, accountId, ct);

    public async Task<IReadOnlyList<WorkshopActivity>> GetActivitiesAsync(string accountId, int page = 1,
        int pageSize = 100, WorkshopOperationType? type = null, CancellationToken ct = default)
    {
        var activities = await repository.ListAsync<WorkshopActivity>(DbCollections.WorkshopActivities, accountId, ct);
        return activities.Where(x => !type.HasValue || x.Type == type.Value)
            .OrderByDescending(x => x.EffectiveAt)
            .Skip((Math.Clamp(page, 1, 10000) - 1) * Math.Clamp(pageSize, 1, 200))
            .Take(Math.Clamp(pageSize, 1, 200))
            .ToArray();
    }

    public async Task<WorkshopDashboardData> GetDashboardAsync(string accountId, CancellationToken ct = default)
    {
        var firearms = await GetFirearmsAsync(accountId, ct);
        var components = await GetComponentsAsync(accountId, ct);
        var batches = await GetBatchesAsync(accountId, ct);
        var allocations = await repository.ListAsync<AmmoAllocation>(DbCollections.WorkshopAmmoAllocations, accountId, ct);
        var activities = await repository.ListAsync<WorkshopActivity>(DbCollections.WorkshopActivities, accountId, ct);
        var reconciliationState = await repository.GetReconciliationAsync(accountId, ct);
        var allocated = allocations.Sum(x => x.ActiveQuantity);
        return new(
            firearms.Count, components.Count, firearms.Sum(x => x.TotalRounds),
            components.Count(x => x.ServiceInterval is > 0 && x.TotalRounds >= x.ServiceInterval),
            batches.Sum(x => x.ProducedQuantity), batches.Sum(x => x.OnHandQuantity) - allocated,
            allocated, activities.Count(x => x.EffectiveAt >= Now().AddDays(-30)),
            reconciliationState?.State ?? WorkshopProjectionState.Current);
    }

    public async Task<ComponentInstallation> InstallComponentAsync(string accountId, WorkshopInstallCommand command,
        CancellationToken ct = default)
    {
        _ = await repository.GetAsync<Firearm>(DbCollections.WorkshopFirearms, accountId, command.FirearmId, ct)
            ?? throw new WorkshopValidationException("Firearm was not found.");
        var component = await repository.GetAsync<ComponentInstance>(DbCollections.WorkshopComponentInstances, accountId, command.ComponentId, ct)
            ?? throw new WorkshopValidationException("Component was not found.");
        if (component.Status == WorkshopComponentStatus.Retired)
            throw new WorkshopValidationException("Retired components cannot be installed.");
        var installationId = StableId(accountId, command.IdempotencyKey, "installation");
        var installationSourceIds = new[] { installationId, command.ComponentId, command.FirearmId };
        var existingOperation = await repository.FindOperationAsync(accountId, command.IdempotencyKey, ct);
        if (existingOperation is not null)
        {
            var expectedHash = PayloadHash(WorkshopOperationType.ComponentInstalled, [],
                installationSourceIds, JsonSerializer.Serialize(command), null);
            if (existingOperation.PayloadHash != expectedHash)
                throw new WorkshopConflictException("Idempotency key was already used for a different operation.");
            return await repository.GetAsync<ComponentInstallation>(
                       DbCollections.WorkshopComponentInstallations, accountId, installationId, ct)
                   ?? throw new WorkshopConflictException("The idempotent installation could not be recovered.");
        }
        var installations = await repository.ListAsync<ComponentInstallation>(
            DbCollections.WorkshopComponentInstallations, accountId, ct);
        if (installations.Any(x => x.ComponentId == command.ComponentId && x.RemovedAt is null))
            throw new WorkshopConflictException("The component is already installed.");
        if (installations.Any(x => x.ComponentId == command.ComponentId &&
                x.InstalledAt < command.InstalledAt && x.RemovedAt > command.InstalledAt))
            throw new WorkshopConflictException("The installation timeline overlaps an existing installation.");

        var installation = await repository.GetAsync<ComponentInstallation>(
            DbCollections.WorkshopComponentInstallations, accountId, installationId, ct);
        if (installation is null)
        {
            installation = new ComponentInstallation
            {
                Id = installationId, AccountId = accountId,
                ComponentId = command.ComponentId, FirearmId = command.FirearmId, InstalledAt = command.InstalledAt,
                InstallationNotes = Trim(command.InstallationNotes), CreationDateTime = Now()
            };
            await repository.AddAsync(DbCollections.WorkshopComponentInstallations, installation, ct);
        }
        var result = await CommitAsync(accountId, command.IdempotencyKey, WorkshopOperationType.ComponentInstalled,
            command.InstalledAt, [], [installation.Id, command.ComponentId, command.FirearmId],
            JsonSerializer.Serialize(command), ct);
        if (!result.Idempotent)
            await repository.ReplaceAsync(DbCollections.WorkshopComponentInstances,
                component with { Status = WorkshopComponentStatus.Installed, Revision = component.Revision + 1 },
                accountId, component.Revision, ct);
        return installation;
    }

    public async Task<ComponentInstallation> RemoveComponentAsync(string accountId, WorkshopRemoveCommand command,
        CancellationToken ct = default)
    {
        var installation = await repository.GetAsync<ComponentInstallation>(
            DbCollections.WorkshopComponentInstallations, accountId, command.InstallationId, ct)
            ?? throw new WorkshopValidationException("Installation was not found.");
        var existingOperation = await repository.FindOperationAsync(accountId, command.IdempotencyKey, ct);
        if (existingOperation is not null)
        {
            var expectedHash = PayloadHash(WorkshopOperationType.ComponentRemoved, [],
                new[] { installation.Id, installation.ComponentId }, JsonSerializer.Serialize(command), null);
            if (existingOperation.PayloadHash != expectedHash)
                throw new WorkshopConflictException("Idempotency key was already used for a different operation.");
            return installation;
        }
        if (installation.RemovedAt is not null)
            throw new WorkshopConflictException("The installation has already been removed.");
        if (command.RemovedAt < installation.InstalledAt)
            throw new WorkshopValidationException("Removal cannot precede installation.");
        var component = await repository.GetAsync<ComponentInstance>(
            DbCollections.WorkshopComponentInstances, accountId, installation.ComponentId, ct)
            ?? throw new WorkshopValidationException("Component was not found.");
        var result = await CommitAsync(accountId, command.IdempotencyKey, WorkshopOperationType.ComponentRemoved,
            command.RemovedAt, [], [installation.Id, installation.ComponentId],
            JsonSerializer.Serialize(command), ct);
        if (!result.Idempotent) {
            var updated = installation with
            {
                RemovedAt = command.RemovedAt, RemovalNotes = Trim(command.RemovalNotes),
                Revision = installation.Revision + 1
            };
            if (!await repository.ReplaceAsync(DbCollections.WorkshopComponentInstallations, updated,
                    accountId, command.ExpectedRevision, ct))
                throw new WorkshopConflictException("Installation changed. Reload before removing it.");
            var stillInstalled = (await repository.ListAsync<ComponentInstallation>(
                DbCollections.WorkshopComponentInstallations, accountId, ct))
                .Any(x => x.ComponentId == component.Id && x.RemovedAt is null);
            if (!stillInstalled)
                await repository.ReplaceAsync(DbCollections.WorkshopComponentInstances,
                    component with { Status = WorkshopComponentStatus.Inventory, Revision = component.Revision + 1 },
                    accountId, component.Revision, ct);
            installation = updated;
        }
        return installation;
    }

    public async Task<MaintenanceEvent> RecordMaintenanceAsync(string accountId, WorkshopMaintenanceCommand command,
        CancellationToken ct = default)
    {
        if (command.FirearmId is null && command.ComponentId is null)
            throw new WorkshopValidationException("A firearm or component is required.");
        if (command.FirearmId is not null)
            _ = await repository.GetAsync<Firearm>(DbCollections.WorkshopFirearms, accountId, command.FirearmId, ct)
                ?? throw new WorkshopValidationException("Firearm was not found.");
        if (command.ComponentId is not null)
            _ = await repository.GetAsync<ComponentInstance>(DbCollections.WorkshopComponentInstances, accountId, command.ComponentId, ct)
                ?? throw new WorkshopValidationException("Component was not found.");
        var eventId = StableId(accountId, command.IdempotencyKey, "maintenance");
        var payload = JsonSerializer.Serialize(command);
        var existingOperation = await repository.FindOperationAsync(accountId, command.IdempotencyKey, ct);
        if (existingOperation is not null)
        {
            var expectedHash = PayloadHash(WorkshopOperationType.MaintenanceRecorded, [],
                [command.FirearmId ?? string.Empty, command.ComponentId ?? string.Empty], payload, null);
            if (existingOperation.PayloadHash != expectedHash)
                throw new WorkshopConflictException("Idempotency key was already used for a different operation.");
        }
        var maintenance = await repository.GetAsync<MaintenanceEvent>(
            DbCollections.WorkshopMaintenanceEvents, accountId, eventId, ct);
        if (maintenance is not null) return maintenance;
        var result = await CommitAsync(accountId, command.IdempotencyKey, WorkshopOperationType.MaintenanceRecorded,
            command.Date, [], [command.FirearmId ?? string.Empty, command.ComponentId ?? string.Empty],
            payload, ct);
        maintenance = new MaintenanceEvent
        {
            Id = eventId, AccountId = accountId, FirearmId = command.FirearmId, ComponentId = command.ComponentId,
            EventType = command.EventType, Date = command.Date, RoundCountAtEvent = command.RoundCountAtEvent,
            Description = command.Description.Trim(), Notes = Trim(command.Notes), CreationDateTime = Now()
        };
        if (!result.Idempotent || existingOperation is not null)
            await repository.AddAsync(DbCollections.WorkshopMaintenanceEvents, maintenance, ct);
        return maintenance;
    }

    public async Task<AmmoRecipe> UpdateRecipeAsync(string accountId, string recipeId, long expectedVersion,
        RecipeSnapshot snapshot, string idempotencyKey, CancellationToken ct = default)
    {
        var recipe = await repository.GetAsync<AmmoRecipe>(DbCollections.WorkshopRecipes, accountId, recipeId, ct)
            ?? throw new WorkshopValidationException("Recipe was not found.");
        if (recipe.Version != expectedVersion) throw new WorkshopConflictException("Recipe changed. Reload before updating.");
        var next = snapshot with { Version = recipe.Version + 1 };
        var result = await CommitAsync(accountId, idempotencyKey, WorkshopOperationType.RecipeUpdated, Now(),
            [], [recipeId], JsonSerializer.Serialize(next), ct);
        if (!result.Idempotent)
        {
            var updated = recipe with
            {
                Name = next.Name, Caliber = next.Caliber, Oal = next.Oal, BulletWeight = next.BulletWeight,
                BulletForm = next.BulletForm, Drawing = next.Drawing, Primer = next.Primer, Powder = next.Powder,
                PowderWeight = next.PowderWeight, Notes = next.Notes, Version = next.Version, UpdatedAt = Now()
            };
            await repository.ReplaceAsync(DbCollections.WorkshopRecipes, updated, accountId, cancellationToken: ct);
            await repository.AddAsync(DbCollections.WorkshopRecipeVersions, new AmmoRecipeVersion
            {
                Id = NewId(), AccountId = accountId, RecipeId = recipeId, Version = next.Version,
                Snapshot = next, CreationDateTime = Now()
            }, ct);
            recipe = updated;
        }
        return recipe;
    }

    public async Task<AmmoRecipe> ArchiveRecipeAsync(string accountId, string recipeId, long expectedVersion,
        string idempotencyKey, CancellationToken ct = default)
    {
        var recipe = await repository.GetAsync<AmmoRecipe>(DbCollections.WorkshopRecipes, accountId, recipeId, ct)
            ?? throw new WorkshopValidationException("Recipe was not found.");
        if (recipe.Version != expectedVersion) throw new WorkshopConflictException("Recipe changed. Reload before archiving.");
        var result = await CommitAsync(accountId, idempotencyKey, WorkshopOperationType.RecipeArchived, Now(),
            [], [recipeId], JsonSerializer.Serialize(new { recipeId, expectedVersion }), ct);
        if (!result.Idempotent)
            recipe = recipe with { Status = WorkshopRecipeStatus.Archived, UpdatedAt = Now() };
        if (!result.Idempotent)
            await repository.ReplaceAsync(DbCollections.WorkshopRecipes, recipe, accountId, cancellationToken: ct);
        return recipe;
    }

    public async Task<Firearm> CreateFirearmAsync(string accountId, WorkshopFirearmCommand command, CancellationToken ct = default)
    {
        RequireAccount(accountId);
        RequireText(command.Manufacturer, "Manufacturer", 120);
        RequireText(command.Model, "Model", 120);
        RequireText(command.Caliber, "Caliber", 80);
        var firearm = new Firearm
        {
            Id = NewId(), AccountId = accountId, Manufacturer = command.Manufacturer.Trim(),
            Model = command.Model.Trim(), Caliber = command.Caliber.Trim(), Category = Trim(command.Category),
            Nickname = Trim(command.Nickname), PurchaseDate = command.PurchaseDate, Notes = Trim(command.Notes),
            CreationDateTime = Now()
        };
        await repository.AddAsync(DbCollections.WorkshopFirearms, firearm, ct);
        await RecordActivityAsync(accountId, WorkshopOperationType.FirearmCreated, firearm.Id, ct);
        return firearm;
    }

    public async Task<Firearm> UpdateFirearmAsync(string accountId, string firearmId,
        WorkshopUpdateFirearmCommand command, CancellationToken ct = default)
    {
        var firearm = await repository.GetAsync<Firearm>(DbCollections.WorkshopFirearms, accountId, firearmId, ct)
            ?? throw new WorkshopValidationException("Firearm was not found.");
        RequireText(command.Manufacturer, "Manufacturer", 120);
        RequireText(command.Model, "Model", 120);
        RequireText(command.Caliber, "Caliber", 80);
        var payload = JsonSerializer.Serialize(command);
        var deltas = Array.Empty<WorkshopDelta>();
        var sourceIds = new[] { firearmId };
        var existing = await repository.FindOperationAsync(accountId, command.IdempotencyKey, ct);
        if (existing is not null)
        {
            if (existing.OperationType != WorkshopOperationType.FirearmUpdated ||
                existing.PayloadHash != PayloadHash(WorkshopOperationType.FirearmUpdated, deltas, sourceIds, payload, null))
                throw new WorkshopConflictException("Idempotency key was already used for a different operation.");
            return firearm;
        }
        if (firearm.Revision != command.ExpectedRevision)
            throw new WorkshopConflictException("Firearm changed. Reload before updating.");
        var result = await CommitAsync(accountId, command.IdempotencyKey, WorkshopOperationType.FirearmUpdated,
            Now(), deltas, sourceIds, payload, ct);
        if (!result.Idempotent)
        {
            var updated = firearm with
            {
                Manufacturer = command.Manufacturer.Trim(), Model = command.Model.Trim(), Caliber = command.Caliber.Trim(),
                Category = Trim(command.Category), Nickname = Trim(command.Nickname), PurchaseDate = command.PurchaseDate,
                Notes = Trim(command.Notes), Revision = firearm.Revision + 1
            };
            if (!await repository.ReplaceAsync(DbCollections.WorkshopFirearms, updated, accountId, firearm.Revision, ct))
                throw new WorkshopConflictException("Firearm changed while updating it.");
            firearm = updated;
        }
        return firearm;
    }

    public async Task<Firearm> ArchiveFirearmAsync(string accountId, string firearmId,
        WorkshopArchiveFirearmCommand command, CancellationToken ct = default)
    {
        var firearm = await repository.GetAsync<Firearm>(DbCollections.WorkshopFirearms, accountId, firearmId, ct)
            ?? throw new WorkshopValidationException("Firearm was not found.");
        var payload = JsonSerializer.Serialize(new { firearmId, command.ExpectedRevision });
        var deltas = Array.Empty<WorkshopDelta>();
        var sourceIds = new[] { firearmId };
        var existing = await repository.FindOperationAsync(accountId, command.IdempotencyKey, ct);
        if (existing is not null)
        {
            if (existing.OperationType != WorkshopOperationType.FirearmArchived ||
                existing.PayloadHash != PayloadHash(WorkshopOperationType.FirearmArchived, deltas, sourceIds, payload, null))
                throw new WorkshopConflictException("Idempotency key was already used for a different operation.");
            return firearm;
        }
        if (firearm.Revision != command.ExpectedRevision)
            throw new WorkshopConflictException("Firearm changed. Reload before archiving.");
        if (firearm.Archived) return firearm;
        var result = await CommitAsync(accountId, command.IdempotencyKey, WorkshopOperationType.FirearmArchived,
            Now(), deltas, sourceIds, payload, ct);
        if (!result.Idempotent)
        {
            var archived = firearm with { Archived = true, Revision = firearm.Revision + 1 };
            if (!await repository.ReplaceAsync(DbCollections.WorkshopFirearms, archived, accountId, firearm.Revision, ct))
                throw new WorkshopConflictException("Firearm changed while archiving it.");
            firearm = archived;
        }
        return firearm;
    }

    public async Task<ComponentDefinition> CreateComponentDefinitionAsync(string accountId, WorkshopComponentDefinitionCommand command, CancellationToken ct = default)
    {
        RequireText(command.Name, "Name", 160);
        var definition = new ComponentDefinition
        {
            Id = NewId(), AccountId = accountId, Name = command.Name.Trim(), Manufacturer = Trim(command.Manufacturer),
            Model = Trim(command.Model), Description = Trim(command.Description), DefaultServiceInterval = command.DefaultServiceInterval,
            TrackingMode = command.TrackingMode, Notes = Trim(command.Notes), CreationDateTime = Now()
        };
        await repository.AddAsync(DbCollections.WorkshopComponentDefinitions, definition, ct);
        await RecordActivityAsync(accountId, WorkshopOperationType.ComponentDefinitionCreated, definition.Id, ct);
        return definition;
    }

    public async Task<ComponentInstance> CreateComponentAsync(string accountId, WorkshopComponentCommand command, CancellationToken ct = default)
    {
        var definition = await repository.GetAsync<ComponentDefinition>(DbCollections.WorkshopComponentDefinitions, accountId, command.DefinitionId, ct)
            ?? throw new WorkshopValidationException("Component definition was not found.");
        var component = new ComponentInstance
        {
            Id = NewId(), AccountId = accountId, DefinitionId = command.DefinitionId,
            IdentificationNote = Trim(command.IdentificationNote), Manufacturer = Trim(command.Manufacturer) ?? definition.Manufacturer,
            Model = Trim(command.Model) ?? definition.Model, ServiceInterval = command.ServiceInterval ?? definition.DefaultServiceInterval,
            TrackingMode = command.TrackingMode ?? definition.TrackingMode, Status = WorkshopComponentStatus.Inventory,
            Notes = Trim(command.Notes), CreationDateTime = Now()
        };
        await repository.AddAsync(DbCollections.WorkshopComponentInstances, component, ct);
        await RecordActivityAsync(accountId, WorkshopOperationType.ComponentCreated, component.Id, ct);
        return component;
    }

    public async Task<AmmoRecipe> CreateRecipeAsync(string accountId, RecipeSnapshot snapshot, CancellationToken ct = default)
    {
        RequireText(snapshot.Name, "Name", 160);
        RequireText(snapshot.Caliber, "Caliber", 80);
        var recipe = new AmmoRecipe
        {
            Id = NewId(), AccountId = accountId, Name = snapshot.Name.Trim(), Caliber = snapshot.Caliber.Trim(),
            Oal = snapshot.Oal, BulletWeight = snapshot.BulletWeight, BulletForm = snapshot.BulletForm,
            Drawing = snapshot.Drawing, Primer = snapshot.Primer, Powder = snapshot.Powder,
            PowderWeight = snapshot.PowderWeight, Notes = snapshot.Notes, UpdatedAt = Now(), CreationDateTime = Now()
        };
        await repository.AddAsync(DbCollections.WorkshopRecipes, recipe, ct);
        await repository.AddAsync(DbCollections.WorkshopRecipeVersions, new AmmoRecipeVersion
        {
            Id = NewId(), AccountId = accountId, RecipeId = recipe.Id, Version = 1, Snapshot = snapshot with { Version = 1 }, CreationDateTime = Now()
        }, ct);
        await RecordActivityAsync(accountId, WorkshopOperationType.RecipeCreated, recipe.Id, ct);
        return recipe;
    }

    public async Task<WorkshopOperationResult> RecordShootingAsync(string accountId, WorkshopShootingCommand command, CancellationToken ct = default)
    {
        if (command.TotalRounds <= 0) throw new WorkshopValidationException("Total rounds must be positive.");
        var firearm = await repository.GetAsync<Firearm>(DbCollections.WorkshopFirearms, accountId, command.FirearmId, ct)
            ?? throw new WorkshopValidationException("Firearm was not found.");
        if (firearm.Archived) throw new WorkshopValidationException("Archived firearms cannot receive shooting sessions.");
        var consumed = command.ConsumptionRecords.Sum(x => x.Quantity);
        if (consumed + command.UntrackedAmmunition != command.TotalRounds)
            throw new WorkshopValidationException("Known consumption plus untracked ammunition must equal total rounds.");
        if (command.UsageSegments.Sum(x => x.Quantity) != command.TotalRounds)
            throw new WorkshopValidationException("Usage segments must equal total rounds.");
        if (command.ConsumptionRecords.Any(x => x.Quantity <= 0))
            throw new WorkshopValidationException("Consumption quantities must be positive.");
        if (command.ConsumptionRecords.Any(x => x.BatchId is null && x.AllocationId is not null))
            throw new WorkshopValidationException("An allocation requires its ammunition batch.");
        if (command.UsageSegments.Any(x => x.Quantity <= 0))
            throw new WorkshopValidationException("Usage segment quantities must be positive.");
        var knownBatchConsumption = command.ConsumptionRecords.Where(x => x.BatchId is not null)
            .GroupBy(x => x.BatchId!, StringComparer.Ordinal)
            .ToArray();

        var deltas = new List<WorkshopDelta> { new(WorkshopAggregateKind.Firearm, firearm.Id, RoundsDelta: command.TotalRounds) };
        foreach (var segment in command.UsageSegments)
        {
            var installation = await repository.GetAsync<ComponentInstallation>(
                DbCollections.WorkshopComponentInstallations, accountId, segment.InstallationId, ct);
            if (installation is null || installation.ComponentId != segment.ComponentId ||
                installation.FirearmId != firearm.Id || installation.InstalledAt > command.Date ||
                installation.RemovedAt <= command.Date)
                throw new WorkshopValidationException("A usage segment does not reference the component installation active at the session date.");
            _ = await repository.GetAsync<ComponentInstance>(
                DbCollections.WorkshopComponentInstances, accountId, segment.ComponentId, ct)
                ?? throw new WorkshopValidationException("A usage segment references an unknown component.");
            deltas.Add(new(WorkshopAggregateKind.Component, segment.ComponentId, RoundsDelta: segment.Quantity,
                ComponentId: segment.ComponentId, InstallationId: segment.InstallationId));
        }
        foreach (var batchGroup in knownBatchConsumption)
        {
            var batch = await repository.GetAsync<AmmoBatch>(DbCollections.WorkshopAmmoBatches, accountId, batchGroup.Key, ct)
                ?? throw new WorkshopValidationException("Ammunition batch was not found.");
            var batchQuantity = batchGroup.Sum(x => x.Quantity);
            if (batchQuantity > batch.OnHandQuantity) throw new WorkshopValidationException("Ammunition batch has insufficient stock.");
            deltas.Add(new(WorkshopAggregateKind.Batch, batch.Id, StockDelta: -batchQuantity, BatchId: batch.Id));
            foreach (var allocationGroup in batchGroup.Where(x => x.AllocationId is not null)
                         .GroupBy(x => x.AllocationId!, StringComparer.Ordinal))
            {
                var allocation = await repository.GetAsync<AmmoAllocation>(DbCollections.WorkshopAmmoAllocations, accountId, allocationGroup.Key, ct)
                    ?? throw new WorkshopValidationException("Ammunition allocation was not found.");
                var allocationQuantity = allocationGroup.Sum(x => x.Quantity);
                if (allocation.BatchId != batch.Id || allocation.ActiveQuantity < allocationQuantity)
                    throw new WorkshopValidationException("Ammunition allocation has insufficient stock.");
                deltas.Add(new(WorkshopAggregateKind.Allocation, allocation.Id, AllocationDelta: -allocationQuantity,
                    AllocationConsumedDelta: allocationQuantity, AllocationId: allocation.Id));
            }
        }

        var operation = await CommitAsync(accountId, command.IdempotencyKey, WorkshopOperationType.ShootingRecorded,
            command.Date, deltas, [command.FirearmId], JsonSerializer.Serialize(command), ct);
        if (!operation.Idempotent)
        {
            await repository.AddAsync(DbCollections.WorkshopShootingSessions, new ShootingSession
            {
                Id = NewId(), AccountId = accountId, FirearmId = command.FirearmId, Date = command.Date,
                TotalRounds = command.TotalRounds, Notes = Trim(command.Notes), ConsumptionRecords = command.ConsumptionRecords,
                UsageSegments = command.UsageSegments, UntrackedAmmunition = command.UntrackedAmmunition,
                CreationDateTime = Now()
            }, ct);
        }
        return operation;
    }

    public async Task<WorkshopOperationResult> RecordProductionAsync(string accountId, WorkshopProductionCommand command, CancellationToken ct = default)
    {
        if (command.Quantity <= 0) throw new WorkshopValidationException("Produced quantity must be positive.");
        var recipe = await repository.GetAsync<AmmoRecipe>(DbCollections.WorkshopRecipes, accountId, command.RecipeId, ct)
            ?? throw new WorkshopValidationException("Recipe was not found.");
        var snapshot = new RecipeSnapshot(recipe.Name, recipe.Caliber, recipe.Oal, recipe.BulletWeight, recipe.BulletForm,
            recipe.Drawing, recipe.Primer, recipe.Powder, recipe.PowderWeight, recipe.Notes, recipe.Version);
        var batchId = StableId(accountId, command.IdempotencyKey, "batch");
        var sessionId = StableId(accountId, command.IdempotencyKey, "production-session");
        var existingOperation = await repository.FindOperationAsync(accountId, command.IdempotencyKey, ct);
        var productionDeltas = new[] { new WorkshopDelta(WorkshopAggregateKind.Batch, batchId, StockDelta: command.Quantity) };
        var productionSourceIds = new[] { batchId, sessionId };
        if (existingOperation is not null)
        {
            var expectedHash = PayloadHash(WorkshopOperationType.ProductionRecorded, productionDeltas,
                productionSourceIds, JsonSerializer.Serialize(command), null);
            if (existingOperation.PayloadHash != expectedHash)
                throw new WorkshopConflictException("Idempotency key was already used for a different operation.");
            return new(existingOperation, true);
        }
        var existingBatch = await repository.GetAsync<AmmoBatch>(DbCollections.WorkshopAmmoBatches, accountId, batchId, ct);
        if (existingBatch is null)
            await repository.AddAsync(DbCollections.WorkshopAmmoBatches, new AmmoBatch
            {
                Id = batchId, AccountId = accountId, RecipeId = recipe.Id, RecipeSnapshot = snapshot,
                ProductionSessionId = sessionId, ProductionDate = command.ProductionDate,
                ProducedQuantity = command.Quantity, OnHandQuantity = 0, Notes = Trim(command.Notes), CreationDateTime = Now()
            }, ct);
        var existingSession = await repository.GetAsync<ProductionSession>(
            DbCollections.WorkshopProductionSessions, accountId, sessionId, ct);
        if (existingSession is null)
            await repository.AddAsync(DbCollections.WorkshopProductionSessions, new ProductionSession
            {
                Id = sessionId, AccountId = accountId, RecipeId = recipe.Id, RecipeSnapshot = snapshot,
                ProductionDate = command.ProductionDate, QuantityProduced = command.Quantity, Notes = Trim(command.Notes),
                BatchId = batchId, CreationDateTime = Now()
            }, ct);
        var result = await CommitAsync(accountId, command.IdempotencyKey, WorkshopOperationType.ProductionRecorded,
            command.ProductionDate, productionDeltas, productionSourceIds, JsonSerializer.Serialize(command), ct);
        return result;
    }

    public async Task<WorkshopOperationResult> AllocateAsync(string accountId, WorkshopAllocationCommand command, CancellationToken ct = default)
    {
        if (command.Quantity <= 0) throw new WorkshopValidationException("Allocation quantity must be positive.");
        var batch = await repository.GetAsync<AmmoBatch>(DbCollections.WorkshopAmmoBatches, accountId, command.BatchId, ct)
            ?? throw new WorkshopValidationException("Ammunition batch was not found.");
        _ = await repository.GetAsync<Firearm>(DbCollections.WorkshopFirearms, accountId, command.FirearmId, ct)
            ?? throw new WorkshopValidationException("Firearm was not found.");
        var allocationId = StableId(accountId, command.IdempotencyKey, "allocation");
        var allocationDelta = new WorkshopDelta(WorkshopAggregateKind.Allocation, allocationId,
            AllocationDelta: command.Quantity);
        var allocationSourceIds = new[] { batch.Id, command.FirearmId };
        var existingOperation = await repository.FindOperationAsync(accountId, command.IdempotencyKey, ct);
        if (existingOperation is not null)
        {
            var expectedHash = PayloadHash(WorkshopOperationType.AllocationRecorded, new[] { allocationDelta },
                allocationSourceIds, JsonSerializer.Serialize(command), null);
            if (existingOperation.PayloadHash != expectedHash)
                throw new WorkshopConflictException("Idempotency key was already used for a different operation.");
            return new(existingOperation, true);
        }
        var existing = (await repository.ListAsync<AmmoAllocation>(DbCollections.WorkshopAmmoAllocations, accountId, ct))
            .Where(x => x.BatchId == batch.Id).Sum(x => x.ActiveQuantity);
        if (existing + command.Quantity > batch.OnHandQuantity) throw new WorkshopValidationException("Allocation exceeds available stock.");
        var existingAllocation = await repository.GetAsync<AmmoAllocation>(
            DbCollections.WorkshopAmmoAllocations, accountId, allocationId, ct);
        if (existingAllocation is null)
            await repository.AddAsync(DbCollections.WorkshopAmmoAllocations, new AmmoAllocation
            {
                Id = allocationId, AccountId = accountId, BatchId = batch.Id, FirearmId = command.FirearmId,
                Quantity = 0, AllocatedAt = Now(), Notes = Trim(command.Notes), CreationDateTime = Now()
            }, ct);
        var result = await CommitAsync(accountId, command.IdempotencyKey, WorkshopOperationType.AllocationRecorded,
            Now(), new[] { allocationDelta }, allocationSourceIds, JsonSerializer.Serialize(command), ct);
        return result;
    }

    public async Task<WorkshopOperationResult> ReleaseAllocationAsync(string accountId, WorkshopReleaseCommand command,
        CancellationToken ct = default)
    {
        var allocation = await repository.GetAsync<AmmoAllocation>(
            DbCollections.WorkshopAmmoAllocations, accountId, command.AllocationId, ct)
            ?? throw new WorkshopValidationException("Allocation was not found.");
        var releaseDelta = new WorkshopDelta(WorkshopAggregateKind.Allocation, allocation.Id,
            AllocationDelta: -command.Quantity, AllocationReleasedDelta: command.Quantity, AllocationId: allocation.Id);
        var existingOperation = await repository.FindOperationAsync(accountId, command.IdempotencyKey, ct);
        if (existingOperation is not null)
        {
            var expectedHash = PayloadHash(WorkshopOperationType.AllocationReleased, new[] { releaseDelta },
                new[] { allocation.Id }, JsonSerializer.Serialize(command), null);
            if (existingOperation.PayloadHash != expectedHash)
                throw new WorkshopConflictException("Idempotency key was already used for a different operation.");
            return new(existingOperation, true);
        }
        if (allocation.Revision != command.ExpectedRevision)
            throw new WorkshopConflictException("Allocation changed. Reload before releasing it.");
        if (command.Quantity <= 0 || command.Quantity > allocation.ActiveQuantity)
            throw new WorkshopValidationException("Release exceeds the active allocation.");
        return await CommitAsync(accountId, command.IdempotencyKey, WorkshopOperationType.AllocationReleased,
            Now(), new[] { releaseDelta }, new[] { allocation.Id },
            JsonSerializer.Serialize(command), ct);
    }

    public async Task<WorkshopOperationResult> ReallocateAsync(string accountId, WorkshopReallocateCommand command,
        CancellationToken ct = default)
    {
        var existingOperation = await repository.FindOperationAsync(accountId, command.IdempotencyKey, ct);
        if (existingOperation is not null)
        {
            var existingSource = await repository.GetAsync<AmmoAllocation>(
                DbCollections.WorkshopAmmoAllocations, accountId, command.SourceAllocationId, ct)
                ?? throw new WorkshopValidationException("Source allocation was not found.");
            var existingDestinationId = StableId(accountId, command.IdempotencyKey, "reallocation");
            var expected = new[]
            {
                new WorkshopDelta(WorkshopAggregateKind.Allocation, existingSource.Id, AllocationDelta: -command.Quantity,
                    AllocationReleasedDelta: command.Quantity, AllocationId: existingSource.Id),
                new WorkshopDelta(WorkshopAggregateKind.Allocation, existingDestinationId, AllocationDelta: command.Quantity,
                    AllocationId: existingDestinationId)
            };
            var hash = PayloadHash(WorkshopOperationType.AllocationReallocated, expected,
                [existingSource.Id, existingDestinationId, command.DestinationFirearmId], JsonSerializer.Serialize(command), null);
            if (existingOperation.PayloadHash != hash)
                throw new WorkshopConflictException("Idempotency key was already used for a different operation.");
            return new(existingOperation, true);
        }
        var source = await repository.GetAsync<AmmoAllocation>(
            DbCollections.WorkshopAmmoAllocations, accountId, command.SourceAllocationId, ct)
            ?? throw new WorkshopValidationException("Source allocation was not found.");
        if (command.Quantity <= 0 || command.Quantity > source.ActiveQuantity)
            throw new WorkshopValidationException("Reallocation exceeds the active allocation.");
        var destinationFirearm = await repository.GetAsync<Firearm>(
            DbCollections.WorkshopFirearms, accountId, command.DestinationFirearmId, ct)
            ?? throw new WorkshopValidationException("Destination firearm was not found.");
        if (destinationFirearm.Archived)
            throw new WorkshopValidationException("Archived firearms cannot receive allocations.");
        var destinationId = StableId(accountId, command.IdempotencyKey, "reallocation");
        var destination = await repository.GetAsync<AmmoAllocation>(
            DbCollections.WorkshopAmmoAllocations, accountId, destinationId, ct);
        if (destination is null)
            await repository.AddAsync(DbCollections.WorkshopAmmoAllocations, new AmmoAllocation
            {
                Id = destinationId, AccountId = accountId, BatchId = source.BatchId,
                FirearmId = command.DestinationFirearmId, AllocatedAt = Now(),
                Quantity = 0, CreationDateTime = Now()
            }, ct);
        return await CommitAsync(accountId, command.IdempotencyKey, WorkshopOperationType.AllocationReallocated,
            Now(),
            [
                new(WorkshopAggregateKind.Allocation, source.Id, AllocationDelta: -command.Quantity,
                    AllocationReleasedDelta: command.Quantity, AllocationId: source.Id),
                new(WorkshopAggregateKind.Allocation, destinationId, AllocationDelta: command.Quantity,
                    AllocationId: destinationId)
            ], [source.Id, destinationId, command.DestinationFirearmId],
            JsonSerializer.Serialize(command), ct);
    }

    public async Task<WorkshopOperationResult> CorrectShootingAsync(string accountId,
        WorkshopCorrectionCommand command, CancellationToken ct = default)
    {
        var original = await repository.GetOperationAsync(
            accountId, command.OriginalOperationId, ct)
            ?? throw new WorkshopValidationException("Original operation was not found.");
        if (original.OperationType != WorkshopOperationType.ShootingRecorded)
            throw new WorkshopValidationException("Only shooting operations can be corrected.");
        var operations = await repository.ListAsync<WorkshopOperation>(DbCollections.WorkshopOperations, accountId, ct);
        var existingOperation = await repository.FindOperationAsync(accountId, command.IdempotencyKey, ct);
        if (existingOperation is null && operations.Any(x => x.OriginalOperationId == original.Id))
            throw new WorkshopConflictException("The shooting operation has already been corrected.");

        var reversal = original.Deltas.Select(x => x with
        {
            RoundsDelta = -x.RoundsDelta,
            StockDelta = -x.StockDelta,
            AllocationDelta = -x.AllocationDelta,
            AllocationConsumedDelta = -x.AllocationConsumedDelta,
            AllocationReleasedDelta = -x.AllocationReleasedDelta
        }).ToList();
        IReadOnlyList<WorkshopDelta> replacementDeltas = command.Replacement is null
            ? []
            : await BuildReplacementDeltasAsync(accountId, command.Replacement, reversal, ct);
        var allDeltas = reversal.Concat(replacementDeltas).ToArray();
        var sourceIds = new List<string> { original.Id };
        if (command.Replacement is not null) sourceIds.Add(command.Replacement.FirearmId);
        if (existingOperation is not null)
        {
            var expectedHash = PayloadHash(WorkshopOperationType.ShootingCorrected, allDeltas, sourceIds,
                JsonSerializer.Serialize(command), original.Id);
            if (existingOperation.PayloadHash != expectedHash)
                throw new WorkshopConflictException("Idempotency key was already used for a different operation.");
            return new(existingOperation, true);
        }
        var result = await CommitAsync(accountId, command.IdempotencyKey, WorkshopOperationType.ShootingCorrected,
            Now(), allDeltas, sourceIds, JsonSerializer.Serialize(command), ct, original.Id);
        if (!result.Idempotent && command.Replacement is not null)
            await repository.AddAsync(DbCollections.WorkshopShootingSessions, new ShootingSession
            {
                Id = StableId(accountId, command.IdempotencyKey, "corrected-shooting"),
                AccountId = accountId, FirearmId = command.Replacement.FirearmId,
                Date = command.Replacement.Date, TotalRounds = command.Replacement.TotalRounds,
                Notes = Trim(command.Replacement.Notes),
                ConsumptionRecords = command.Replacement.ConsumptionRecords,
                UsageSegments = command.Replacement.UsageSegments,
                UntrackedAmmunition = command.Replacement.UntrackedAmmunition,
                CorrectionOfId = original.Id, CreationDateTime = Now()
            }, ct);
        return result;
    }

    public Task<WorkshopReconciliationResult> ReconcileAsync(string accountId, bool repair, CancellationToken ct = default) =>
        reconciliation.ReconcileAsync(accountId, repair, ct);

    private async Task<IReadOnlyList<WorkshopDelta>> BuildReplacementDeltasAsync(
        string accountId, WorkshopShootingCommand command, IReadOnlyList<WorkshopDelta> reversal,
        CancellationToken ct)
    {
        if (command.TotalRounds <= 0 ||
            command.ConsumptionRecords.Sum(x => x.Quantity) + command.UntrackedAmmunition != command.TotalRounds ||
            command.UsageSegments.Sum(x => x.Quantity) != command.TotalRounds)
            throw new WorkshopValidationException("Replacement shooting quantities are inconsistent.");
        if (command.ConsumptionRecords.Any(x => x.Quantity <= 0) ||
            command.UsageSegments.Any(x => x.Quantity <= 0) ||
            command.ConsumptionRecords.Any(x => x.BatchId is null && x.AllocationId is not null))
            throw new WorkshopValidationException("Replacement shooting quantities are invalid.");
        var replay = WorkshopReplay.Rebuild(
            (await repository.ListAsync<WorkshopOperation>(DbCollections.WorkshopOperations, accountId, ct))
            .Concat([new WorkshopOperation { Deltas = reversal }]));
        var replacementFirearm = await repository.GetAsync<Firearm>(
            DbCollections.WorkshopFirearms, accountId, command.FirearmId, ct)
            ?? throw new WorkshopValidationException("Replacement firearm was not found.");
        if (replacementFirearm.Archived)
            throw new WorkshopValidationException("Archived firearms cannot receive shooting sessions.");
        var deltas = new List<WorkshopDelta>
        {
            new(WorkshopAggregateKind.Firearm, command.FirearmId, RoundsDelta: command.TotalRounds)
        };
        foreach (var segment in command.UsageSegments)
        {
            var installation = await repository.GetAsync<ComponentInstallation>(
                DbCollections.WorkshopComponentInstallations, accountId, segment.InstallationId, ct);
            if (installation is null || installation.ComponentId != segment.ComponentId ||
                installation.FirearmId != command.FirearmId || installation.InstalledAt > command.Date ||
                installation.RemovedAt <= command.Date)
                throw new WorkshopValidationException("Replacement segment has no matching historical installation.");
            deltas.Add(new(WorkshopAggregateKind.Component, segment.ComponentId,
                RoundsDelta: segment.Quantity, ComponentId: segment.ComponentId, InstallationId: segment.InstallationId));
        }
        foreach (var batchGroup in command.ConsumptionRecords.Where(x => x.BatchId is not null)
                     .GroupBy(x => x.BatchId!, StringComparer.Ordinal))
        {
            var batch = await repository.GetAsync<AmmoBatch>(
                DbCollections.WorkshopAmmoBatches, accountId, batchGroup.Key, ct)
                ?? throw new WorkshopValidationException("Replacement batch was not found.");
            var batchQuantity = batchGroup.Sum(x => x.Quantity);
            if (batchQuantity > replay.For(WorkshopAggregateKind.Batch, batch.Id).Stock)
                throw new WorkshopValidationException("Replacement ammunition exceeds available stock after correction.");
            deltas.Add(new(WorkshopAggregateKind.Batch, batch.Id, StockDelta: -batchQuantity, BatchId: batch.Id));
            foreach (var allocationGroup in batchGroup.Where(x => x.AllocationId is not null)
                         .GroupBy(x => x.AllocationId!, StringComparer.Ordinal))
            {
                var allocation = await repository.GetAsync<AmmoAllocation>(
                    DbCollections.WorkshopAmmoAllocations, accountId, allocationGroup.Key, ct)
                    ?? throw new WorkshopValidationException("Replacement allocation was not found.");
                var quantity = allocationGroup.Sum(x => x.Quantity);
                if (allocation.BatchId != batch.Id)
                    throw new WorkshopValidationException("Replacement allocation does not belong to its batch.");
                if (quantity > replay.For(WorkshopAggregateKind.Allocation, allocation.Id).Allocated)
                    throw new WorkshopValidationException("Replacement ammunition exceeds the active allocation after correction.");
                deltas.Add(new(WorkshopAggregateKind.Allocation, allocation.Id,
                    AllocationDelta: -quantity, AllocationConsumedDelta: quantity, AllocationId: allocation.Id));
            }
        }
        return deltas;
    }

    private async Task<WorkshopOperationResult> CommitAsync(string accountId, string key, WorkshopOperationType type,
        DateTime effectiveAt, IReadOnlyList<WorkshopDelta> deltas, IReadOnlyList<string> sourceIds,
        string payloadIdentity, CancellationToken ct, string? originalOperationId = null)
    {
        RequireText(key, "Idempotency key", 80);
        var hash = PayloadHash(type, deltas, sourceIds, payloadIdentity, originalOperationId);
        var existing = await repository.FindOperationAsync(accountId, key, ct);
        if (existing is not null)
        {
            if (existing.PayloadHash != hash) throw new WorkshopConflictException("Idempotency key was already used for a different operation.");
            await ApplyProjectionAsync(accountId, existing.Deltas, ct);
            return new(existing, true);
        }
        var operation = new WorkshopOperation
        {
            Id = NewId(), AccountId = accountId, ClientIdempotencyKey = key, PayloadHash = hash,
            OperationType = type, EffectiveAt = effectiveAt, RecordedAt = Now(), Deltas = deltas,
            SourceEntityIds = sourceIds, OriginalOperationId = originalOperationId,
            ContributionDigest = WorkshopReplay.Digest(deltas), CreationDateTime = Now()
        };
        try { await repository.AddOperationAsync(operation, ct); }
        catch (WorkshopDuplicateOperationException)
        {
            existing = await repository.FindOperationAsync(accountId, key, ct)
                ?? throw new WorkshopConflictException("The idempotent operation could not be recovered.");
            if (existing.PayloadHash != hash) throw new WorkshopConflictException("Idempotency key conflict.");
            await ApplyProjectionAsync(accountId, existing.Deltas, ct);
            return new(existing, true);
        }
        await ApplyProjectionAsync(accountId, deltas, ct);
        await RecordActivityAsync(accountId, type, operation.Id, ct, operation);
        return new(operation, false);
    }

    private async Task ApplyProjectionAsync(string accountId, IReadOnlyList<WorkshopDelta> deltas, CancellationToken ct)
    {
        var operations = await repository.ListAsync<WorkshopOperation>(DbCollections.WorkshopOperations, accountId, ct);
        var replay = WorkshopReplay.Rebuild(operations);
        foreach (var group in deltas.GroupBy(x => (x.AggregateKind, x.AggregateId)))
        {
            var value = replay.For(group.Key.AggregateKind, group.Key.AggregateId);
            switch (group.Key.AggregateKind)
            {
                case WorkshopAggregateKind.Firearm:
                    var firearm = await repository.GetAsync<Firearm>(DbCollections.WorkshopFirearms, accountId, group.Key.AggregateId, ct);
                    if (firearm is not null && !await repository.ReplaceAsync(DbCollections.WorkshopFirearms,
                            firearm with { TotalRounds = value.Rounds, Revision = firearm.Revision + 1 },
                            accountId, firearm.Revision, ct))
                        throw new WorkshopConflictException("The firearm projection changed while applying the operation.");
                    break;
                case WorkshopAggregateKind.Component:
                    var component = await repository.GetAsync<ComponentInstance>(DbCollections.WorkshopComponentInstances, accountId, group.Key.AggregateId, ct);
                    if (component is not null && !await repository.ReplaceAsync(DbCollections.WorkshopComponentInstances,
                            component with { TotalRounds = value.Rounds, Revision = component.Revision + 1 },
                            accountId, component.Revision, ct))
                        throw new WorkshopConflictException("The component projection changed while applying the operation.");
                    break;
                case WorkshopAggregateKind.Batch:
                    var batch = await repository.GetAsync<AmmoBatch>(DbCollections.WorkshopAmmoBatches, accountId, group.Key.AggregateId, ct);
                    if (batch is not null && !await repository.ReplaceAsync(DbCollections.WorkshopAmmoBatches,
                            batch with { OnHandQuantity = value.Stock, Revision = batch.Revision + 1 },
                            accountId, batch.Revision, ct))
                        throw new WorkshopConflictException("The ammunition batch projection changed while applying the operation.");
                    break;
                case WorkshopAggregateKind.Allocation:
                    var allocation = await repository.GetAsync<AmmoAllocation>(DbCollections.WorkshopAmmoAllocations, accountId, group.Key.AggregateId, ct);
                    if (allocation is not null && !await repository.ReplaceAsync(DbCollections.WorkshopAmmoAllocations,
                            allocation with
                            {
                                Quantity = value.OriginalAllocation,
                                ConsumedQuantity = value.ConsumedAllocation,
                                ReleasedQuantity = value.ReleasedAllocation,
                                Revision = allocation.Revision + 1
                            }, accountId, allocation.Revision, ct))
                        throw new WorkshopConflictException("The ammunition allocation projection changed while applying the operation.");
                    break;
            }
        }
    }

    private async Task RecordActivityAsync(string accountId, WorkshopOperationType type, string sourceId,
        CancellationToken ct, WorkshopOperation? operation = null)
    {
        await repository.AddAsync(DbCollections.WorkshopActivities, new WorkshopActivity
        {
            Id = NewId(), AccountId = accountId, OperationId = operation?.Id ?? sourceId, Type = type,
            EffectiveAt = operation?.EffectiveAt ?? Now(), SourceEntityIds = [sourceId], CreationDateTime = Now()
        }, ct);
    }

    private DateTime Now() => timeProvider.GetUtcNow().UtcDateTime;
    private static string NewId() => ObjectId.GenerateNewId().ToString();
    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static void RequireAccount(string value) { if (string.IsNullOrWhiteSpace(value)) throw new WorkshopValidationException("Account is required."); }
    private static void RequireText(string? value, string name, int max)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max) throw new WorkshopValidationException($"{name} is required and must be at most {max} characters.");
    }
    private static string PayloadHash(WorkshopOperationType type, IReadOnlyList<WorkshopDelta> deltas,
        IReadOnlyList<string> ids, string payloadIdentity, string? originalOperationId) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(new { type, deltas, ids, payloadIdentity, originalOperationId })))).ToLowerInvariant();

    private static string StableId(string accountId, string idempotencyKey, string purpose)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{accountId}:{purpose}:{idempotencyKey}"));
        return Convert.ToHexString(bytes)[..24].ToLowerInvariant();
    }
}
