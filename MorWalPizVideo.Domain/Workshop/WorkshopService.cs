using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MongoDB.Bson;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models.Workshop;

namespace MorWalPizVideo.Domain.Workshop;

public sealed record WorkshopFirearmCommand(string Manufacturer, string Model, string Caliber, string? Category,
    string? Nickname, DateTime? PurchaseDate, string? Notes, long InitialRounds = 0, string? IdempotencyKey = null);
public sealed record WorkshopUpdateFirearmCommand(string Manufacturer, string Model, string Caliber, string? Category,
    string? Nickname, DateTime? PurchaseDate, string? Notes, long ExpectedRevision, string IdempotencyKey);
public sealed record WorkshopArchiveFirearmCommand(long ExpectedRevision, string IdempotencyKey);
public sealed record WorkshopComponentDefinitionCommand(string Name, string? Manufacturer, string? Model,
    string? Description, long? DefaultServiceInterval, WorkshopTrackingMode TrackingMode, string? Notes,
    IReadOnlyList<string>? CompatibilityTags = null);
public sealed record WorkshopComponentCommand(string DefinitionId, string? IdentificationNote, string? Manufacturer,
    string? Model, long? ServiceInterval, WorkshopTrackingMode? TrackingMode, string? Notes,
    IReadOnlyList<string>? CompatibilityTags = null, long InitialRounds = 0, string? IdempotencyKey = null);
public sealed record WorkshopCreateAndInstallCommand(string DefinitionId, string? FirearmId, string? IdentificationNote,
    string? Manufacturer, string? Model, long? ServiceInterval, WorkshopTrackingMode? TrackingMode, string? Notes,
    DateTime InstalledAt, string? InstallationNotes, string IdempotencyKey,
    IReadOnlyList<string>? CompatibilityTags = null, long InitialRounds = 0);
public sealed record WorkshopUpdateComponentDefinitionCommand(string Name, string? Description, string? Notes, long ExpectedRevision, string IdempotencyKey);
public sealed record WorkshopArchiveComponentDefinitionCommand(long ExpectedRevision, string IdempotencyKey);
public sealed record WorkshopUpdateComponentCommand(string? IdentificationNote, string? Manufacturer, string? Model,
    long? ServiceInterval, WorkshopTrackingMode TrackingMode, string? Notes, IReadOnlyList<string>? CompatibilityTags,
    long ExpectedRevision, string IdempotencyKey, long? TotalRounds = null);
public sealed record WorkshopArchiveComponentCommand(long ExpectedRevision, string IdempotencyKey);
public sealed record WorkshopInstallCommand(string ComponentId, string FirearmId, DateTime InstalledAt,
    string? InstallationNotes, string IdempotencyKey);
public sealed record WorkshopRemoveCommand(string InstallationId, DateTime RemovedAt,
    string? RemovalNotes, long ExpectedRevision, string IdempotencyKey);
public sealed record WorkshopMaintenanceCommand(string? FirearmId, string? ComponentId,
    WorkshopMaintenanceEventType EventType, DateTime Date, long? RoundCountAtEvent,
    string Description, string? Notes, string IdempotencyKey);
public sealed record WorkshopShootingCommand(string FirearmId, DateTime Date, long TotalRounds, string? Notes,
    IReadOnlyList<ShootingRecipeConsumption> RecipeConsumptions, long UntrackedAmmunition,
    IReadOnlyList<ShootingUsageSegment> UsageSegments, string IdempotencyKey);
public sealed record WorkshopProductionCommand(string RecipeId, DateTime ProductionDate, long Quantity, string? Notes, string IdempotencyKey);
public sealed record WorkshopAllocationCommand(string BatchId, string FirearmId, long Quantity, string? Notes, string IdempotencyKey);
public sealed record WorkshopReleaseCommand(string AllocationId, long Quantity, long ExpectedRevision, string IdempotencyKey);
public sealed record WorkshopReallocateCommand(string SourceAllocationId, string DestinationFirearmId,
    long Quantity, string IdempotencyKey);
public sealed record WorkshopCorrectionCommand(string OriginalOperationId, WorkshopShootingCommand? Replacement,
    string IdempotencyKey);
public sealed record WorkshopDashboardData(int FirearmCount, int ComponentCount, long TotalFirearmRounds,
    int ComponentsAtOrAboveInterval, long TotalProducedRounds, long AvailableRounds,
    int RecentActivityCount, WorkshopProjectionState ProjectionState);
public sealed record WorkshopAnalyticsBucketData(DateTime Date, long ShootingRounds, long ProducedRounds,
    int ShootingSessions, IReadOnlyList<string> Notes);
public sealed record WorkshopAnalyticsData(DateTime From, DateTime To, long ShootingRounds, int ShootingSessions,
    long TrackedRounds, long UntrackedRounds, long ProducedRounds, long AvailableRounds,
    int MaintenanceEvents, int MalfunctionEvents, int CorrectionCount, WorkshopProjectionState ProjectionState,
    IReadOnlyList<WorkshopAnalyticsBucketData> Buckets);
public sealed record WorkshopReportData(WorkshopAnalyticsData Analytics, IReadOnlyList<WorkshopActivity> Activities);
public sealed record WorkshopOperationResult(WorkshopOperation Operation, bool Idempotent);
sealed record WorkshopReplacementResolution(
    IReadOnlyList<WorkshopDelta> Deltas, IReadOnlyList<ShootingConsumption> ConsumptionRecords);
public sealed record WorkshopFirearmDetailData(
    Firearm Firearm,
    IReadOnlyList<(ComponentInstance Component, ComponentDefinition Definition, ComponentInstallation Installation)> Components,
    IReadOnlyList<MaintenanceEvent> Maintenance,
    bool MaintenanceHasMore,
    IReadOnlyList<WorkshopShootingSessionData> Shooting,
    bool ShootingHasMore,
    IReadOnlyList<WorkshopRoundHistoryPointData> RoundHistory,
    IReadOnlyList<ComponentInstance> ReplacementCandidates,
    WorkshopProjectionState ProjectionState,
    int MalfunctionCount);
public sealed record WorkshopRoundHistoryPointData(DateTime EffectiveAt, long Delta, long CumulativeRounds, string OperationId);
public sealed record ShootingRecipeConsumption(string RecipeId, long Quantity);
public sealed record WorkshopShootingSessionData(ShootingSession Session, bool Superseded);

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

    public async Task<WorkshopFirearmDetailData?> GetFirearmDetailAsync(
        string accountId, string firearmId, int maintenancePage = 1, int shootingPage = 1,
        int pageSize = 15, CancellationToken ct = default)
    {
        var firearm = await repository.GetAsync<Firearm>(DbCollections.WorkshopFirearms, accountId, firearmId, ct);
        if (firearm is null) return null;

        var components = await GetComponentsAsync(accountId, ct);
        var definitions = await GetComponentDefinitionsAsync(accountId, ct);
        var installations = await repository.ListAsync<ComponentInstallation>(
            DbCollections.WorkshopComponentInstallations, accountId, ct);
        var current = installations.Where(x => x.FirearmId == firearmId && x.RemovedAt is null)
            .Join(components, x => x.ComponentId, x => x.Id, (installation, component) => (installation, component))
            .Join(definitions, x => x.component.DefinitionId, x => x.Id,
                (x, definition) => (x.component, definition, x.installation))
            .ToArray();

        var allMaintenance = (await repository.ListAsync<MaintenanceEvent>(
                DbCollections.WorkshopMaintenanceEvents, accountId, ct))
            .Where(x => x.FirearmId == firearmId ||
                x.ComponentId is not null && installations.Any(i =>
                    i.FirearmId == firearmId && i.ComponentId == x.ComponentId &&
                    i.InstalledAt <= x.Date && (i.RemovedAt is null || x.Date <= i.RemovedAt)))
            .OrderByDescending(x => x.Date).ThenByDescending(x => x.Id)
            .ToArray();
        var allShooting = (await repository.ListAsync<ShootingSession>(
                DbCollections.WorkshopShootingSessions, accountId, ct))
            .Where(x => x.FirearmId == firearmId)
            .OrderByDescending(x => x.Date).ThenByDescending(x => x.Id)
            .ToArray();
        var correctionOperations = (await repository.ListAsync<WorkshopOperation>(
                DbCollections.WorkshopOperations, accountId, ct))
            .Where(x => x.OriginalOperationId is not null)
            .Select(x => x.OriginalOperationId!)
            .ToHashSet(StringComparer.Ordinal);
        var operations = (await repository.ListAsync<WorkshopOperation>(
                DbCollections.WorkshopOperations, accountId, ct))
            .Where(x => x.Deltas.Any(d => d.AggregateKind == WorkshopAggregateKind.Firearm &&
                d.AggregateId == firearmId))
            .OrderBy(x => x.EffectiveAt).ThenBy(x => x.RecordedAt).ThenBy(x => x.Id)
            .ToArray();
        long cumulative = 0;
        var roundHistory = operations.Select(operation =>
        {
            var delta = operation.Deltas.Where(d => d.AggregateKind == WorkshopAggregateKind.Firearm &&
                d.AggregateId == firearmId).Sum(d => d.RoundsDelta);
            cumulative += delta;
            return new WorkshopRoundHistoryPointData(operation.EffectiveAt, delta, cumulative, operation.Id);
        }).ToArray();
        var size = Math.Clamp(pageSize, 1, 100);
        var maintenanceOffset = (Math.Clamp(maintenancePage, 1, 10000) - 1) * size;
        var shootingOffset = (Math.Clamp(shootingPage, 1, 10000) - 1) * size;
        var maintenance = allMaintenance.Skip(maintenanceOffset).Take(size).ToArray();
        var shooting = allShooting.Skip(shootingOffset).Take(size)
            .Select(x => new WorkshopShootingSessionData(
                x, x.OperationId is not null && correctionOperations.Contains(x.OperationId)))
            .ToArray();
        var candidates = components.Where(x => x.Status == WorkshopComponentStatus.Inventory && !x.Archived)
            .Where(x => current.All(c => c.component.Id != x.Id || c.component.DefinitionId == x.DefinitionId))
            .ToArray();
        var state = (await repository.GetReconciliationAsync(accountId, ct))?.State ?? WorkshopProjectionState.Current;
        return new(
            firearm,
            current.Select(x => (x.component, x.definition, x.installation)).ToArray(),
            maintenance,
            maintenanceOffset + maintenance.Length < allMaintenance.Length,
            shooting,
            shootingOffset + shooting.Length < allShooting.Length,
            roundHistory, candidates, state,
            allMaintenance.Count(x => x.EventType == WorkshopMaintenanceEventType.Malfunction));
    }

    public Task<IReadOnlyList<ComponentInstance>> GetComponentsAsync(string accountId, CancellationToken ct = default) =>
        repository.ListAsync<ComponentInstance>(DbCollections.WorkshopComponentInstances, accountId, ct);

    public async Task<IReadOnlyList<ComponentInstallation>> GetComponentInstallationsAsync(
        string accountId, CancellationToken ct = default) =>
        (await repository.ListAsync<ComponentInstallation>(
            DbCollections.WorkshopComponentInstallations, accountId, ct))
        .Where(x => x.RemovedAt is null)
        .ToArray();

    public Task<IReadOnlyList<AmmoRecipe>> GetRecipesAsync(string accountId, CancellationToken ct = default) =>
        repository.ListAsync<AmmoRecipe>(DbCollections.WorkshopRecipes, accountId, ct);

    public async Task<AmmoRecipe?> GetActiveRecipeAsync(
        string accountId, string recipeId, CancellationToken ct = default)
    {
        var recipe = await repository.GetAsync<AmmoRecipe>(
           DbCollections.WorkshopRecipes, accountId, recipeId, ct);
        return recipe?.Status == WorkshopRecipeStatus.Active ? recipe : null;
    }

    public Task<IReadOnlyList<AmmoBatch>> GetBatchesAsync(string accountId, CancellationToken ct = default) =>
        repository.ListAsync<AmmoBatch>(DbCollections.WorkshopAmmoBatches, accountId, ct);

    public async Task<IReadOnlyList<WorkshopShootingSessionData>> GetShootingSessionsAsync(
        string accountId, CancellationToken ct = default)
    {
        var sessions = await repository.ListAsync<ShootingSession>(
            DbCollections.WorkshopShootingSessions, accountId, ct);
        var operations = await repository.ListAsync<WorkshopOperation>(
            DbCollections.WorkshopOperations, accountId, ct);
        var superseded = operations
            .Where(x => x.OriginalOperationId is not null)
            .Select(x => x.OriginalOperationId!)
            .ToHashSet(StringComparer.Ordinal);
        return sessions
            .OrderByDescending(x => x.Date).ThenByDescending(x => x.Id)
            .Select(x => new WorkshopShootingSessionData(x,
                x.OperationId is not null && superseded.Contains(x.OperationId)))
            .ToArray();
    }

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
        var activities = await repository.ListAsync<WorkshopActivity>(DbCollections.WorkshopActivities, accountId, ct);
        var reconciliationState = await repository.GetReconciliationAsync(accountId, ct);
        return new(
            firearms.Count, components.Count, firearms.Sum(x => x.TotalRounds),
            components.Count(x => x.ServiceInterval is > 0 && x.TotalRounds >= x.ServiceInterval),
            batches.Sum(x => x.ProducedQuantity), batches.Sum(x => x.OnHandQuantity),
            activities.Count(x => x.EffectiveAt >= Now().AddDays(-30)),
            reconciliationState?.State ?? WorkshopProjectionState.Current);
    }

    public async Task<WorkshopAnalyticsData> GetAnalyticsAsync(string accountId, DateTime fromInclusive,
        DateTime toExclusive, CancellationToken ct = default)
    {
        RequireAccount(accountId);
        if (toExclusive <= fromInclusive)
            throw new WorkshopValidationException("The analytics period must end after it starts.");
        if (toExclusive - fromInclusive > TimeSpan.FromDays(366))
            throw new WorkshopValidationException("The analytics period cannot exceed one year.");

        var sessions = await repository.ListByDateAsync<ShootingSession>(
            DbCollections.WorkshopShootingSessions, accountId, "date", fromInclusive, toExclusive, ct);
        var batches = await repository.ListByDateAsync<AmmoBatch>(
            DbCollections.WorkshopAmmoBatches, accountId, "productionDate", fromInclusive, toExclusive, ct);
        var maintenance = await repository.ListByDateAsync<MaintenanceEvent>(
            DbCollections.WorkshopMaintenanceEvents, accountId, "date", fromInclusive, toExclusive, ct);
        var correctionOperations = await repository.ListOperationsByTypeAsync(
            accountId, WorkshopOperationType.ShootingCorrected, ct);
        var allBatches = await GetBatchesAsync(accountId, ct);

        var supersededOperationIds = correctionOperations
            .Where(x => x.OriginalOperationId is not null)
            .Select(x => x.OriginalOperationId!)
            .ToHashSet(StringComparer.Ordinal);
        var activeSessions = sessions
            .Where(x => x.OperationId is null || !supersededOperationIds.Contains(x.OperationId))
            .ToArray();
        var shootingRounds = activeSessions.Sum(x => x.TotalRounds);
        var untrackedRounds = activeSessions.Sum(x => x.UntrackedAmmunition);
        var buckets = activeSessions.GroupBy(x => x.Date.Date)
            .Select(group => new WorkshopAnalyticsBucketData(group.Key, group.Sum(x => x.TotalRounds), 0, group.Count(),
                group.Select(x => x.Notes).Where(note => !string.IsNullOrWhiteSpace(note)).Select(note => note!.Trim()).ToArray()))
            .Concat(batches.GroupBy(x => x.ProductionDate.Date)
                .Select(group => new WorkshopAnalyticsBucketData(group.Key, 0, group.Sum(x => x.ProducedQuantity), 0,
                    group.Select(x => x.Notes).Where(note => !string.IsNullOrWhiteSpace(note)).Select(note => note!.Trim()).ToArray())))
            .GroupBy(x => x.Date)
            .OrderBy(x => x.Key)
            .Select(group => new WorkshopAnalyticsBucketData(group.Key, group.Sum(x => x.ShootingRounds),
                group.Sum(x => x.ProducedRounds), group.Sum(x => x.ShootingSessions),
                group.SelectMany(x => x.Notes).ToArray()))
            .ToArray();
        var reconciliationState = await repository.GetReconciliationAsync(accountId, ct);
        return new(fromInclusive, toExclusive, shootingRounds, activeSessions.Length,
            shootingRounds - untrackedRounds, untrackedRounds, batches.Sum(x => x.ProducedQuantity),
            allBatches.Sum(x => x.OnHandQuantity), maintenance.Count,
            maintenance.Count(x => x.EventType == WorkshopMaintenanceEventType.Malfunction),
            correctionOperations.Count(x => x.CreationDateTime >= fromInclusive && x.CreationDateTime < toExclusive),
            reconciliationState?.State ?? WorkshopProjectionState.Current, buckets);
    }

    public async Task<WorkshopReportData> GetReportAsync(string accountId, DateTime fromInclusive,
        DateTime toExclusive, CancellationToken ct = default)
    {
        var analytics = await GetAnalyticsAsync(accountId, fromInclusive, toExclusive, ct);
        var activities = await repository.ListByDateAsync<WorkshopActivity>(
            DbCollections.WorkshopActivities, accountId, "effectiveAt", fromInclusive, toExclusive, ct);
        return new(analytics, activities.OrderBy(x => x.EffectiveAt).ThenBy(x => x.Id).ToArray());
    }

    public async Task<ComponentInstallation> InstallComponentAsync(string accountId, WorkshopInstallCommand command,
        CancellationToken ct = default)
    {
        var firearm = await repository.GetAsync<Firearm>(
            DbCollections.WorkshopFirearms, accountId, command.FirearmId, ct)
            ?? throw new WorkshopValidationException("Firearm was not found.");
        if (firearm.Archived)
            throw new WorkshopValidationException("Archived firearms cannot receive components.");
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

    public async Task<(ComponentInstance Component, ComponentInstallation Installation)> CreateAndInstallComponentAsync(
        string accountId, WorkshopCreateAndInstallCommand command, CancellationToken ct = default)
    {
        if (command.InitialRounds < 0)
            throw new WorkshopValidationException("Initial rounds cannot be negative.");
        Firearm? firearm = null;
        if (!string.IsNullOrWhiteSpace(command.FirearmId))
        {
            firearm = await repository.GetAsync<Firearm>(DbCollections.WorkshopFirearms, accountId, command.FirearmId, ct)
                ?? throw new WorkshopValidationException("Firearm was not found.");
            if (firearm.Archived)
                throw new WorkshopValidationException("Archived firearms cannot receive components.");
        }
        var definition = await repository.GetAsync<ComponentDefinition>(
            DbCollections.WorkshopComponentDefinitions, accountId, command.DefinitionId, ct)
            ?? throw new WorkshopValidationException("Component definition was not found.");
        var componentId = StableId(accountId, command.IdempotencyKey, "component");
        var installationId = StableId(accountId, command.IdempotencyKey, "installation");
        var payload = JsonSerializer.Serialize(command);
        var sourceIds = new List<string> { componentId, installationId, command.DefinitionId };
        if (command.FirearmId is not null) sourceIds.Add(command.FirearmId);
        var expectedHash = PayloadHash(WorkshopOperationType.ComponentInstalled, [], sourceIds, payload, null);
        var existing = await repository.FindOperationAsync(accountId, command.IdempotencyKey, ct);
        if (existing is not null && existing.PayloadHash != expectedHash)
            throw new WorkshopConflictException("Idempotency key was already used for a different operation.");
        var component = await repository.GetAsync<ComponentInstance>(
            DbCollections.WorkshopComponentInstances, accountId, componentId, ct);
        if (component is null)
        {
            component = new ComponentInstance
            {
                Id = componentId, AccountId = accountId, DefinitionId = definition.Id,
                IdentificationNote = Trim(command.IdentificationNote),
                Manufacturer = Trim(command.Manufacturer),
                Model = Trim(command.Model),
                ServiceInterval = ValidateInterval(command.ServiceInterval, command.TrackingMode ?? WorkshopTrackingMode.Counter),
                TrackingMode = command.TrackingMode ?? WorkshopTrackingMode.Counter,
                CompatibilityTags = NormalizeTags(command.CompatibilityTags),
                Status = firearm is null ? WorkshopComponentStatus.Inventory : WorkshopComponentStatus.Installed,
                Notes = Trim(command.Notes), CreationDateTime = Now()
            };
            await repository.AddAsync(DbCollections.WorkshopComponentInstances, component, ct);
        }
        var installation = await repository.GetAsync<ComponentInstallation>(
            DbCollections.WorkshopComponentInstallations, accountId, installationId, ct);
        if (firearm is not null && installation is null)
        {
            installation = new ComponentInstallation
            {
                Id = installationId, AccountId = accountId, ComponentId = component.Id,
                FirearmId = command.FirearmId!, InstalledAt = command.InstalledAt,
                InstallationNotes = Trim(command.InstallationNotes), CreationDateTime = Now()
            };
            await repository.AddAsync(DbCollections.WorkshopComponentInstallations, installation, ct);
        }
        if (existing is null)
        {
            await CommitAsync(accountId, command.IdempotencyKey, WorkshopOperationType.ComponentInstalled,
                command.InstalledAt, [], sourceIds, payload, ct);
        }
        if (command.InitialRounds > 0 && existing is null)
            await CommitAsync(accountId, $"{command.IdempotencyKey}:rounds", WorkshopOperationType.ComponentCreated,
                command.InstalledAt, [new(WorkshopAggregateKind.Component, componentId, RoundsDelta: command.InitialRounds)],
                [componentId], JsonSerializer.Serialize(new { command.InitialRounds }), ct);
        component = await repository.GetAsync<ComponentInstance>(
            DbCollections.WorkshopComponentInstances, accountId, componentId, ct) ?? component;
        return (component, installation);
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
        if ((command.FirearmId is null) == (command.ComponentId is null))
            throw new WorkshopValidationException("Exactly one firearm or component is required.");
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
        var operations = await repository.ListAsync<WorkshopOperation>(
            DbCollections.WorkshopOperations, accountId, ct);
        var targetKind = command.FirearmId is not null ? WorkshopAggregateKind.Firearm : WorkshopAggregateKind.Component;
        var targetId = command.FirearmId ?? command.ComponentId!;
        var derivedRoundCount = operations
            .Where(x => x.EffectiveAt <= command.Date)
            .SelectMany(x => x.Deltas)
            .Where(x => x.AggregateKind == targetKind && x.AggregateId == targetId)
            .Sum(x => x.RoundsDelta);
        var result = await CommitAsync(accountId, command.IdempotencyKey, WorkshopOperationType.MaintenanceRecorded,
            command.Date, [], [command.FirearmId ?? string.Empty, command.ComponentId ?? string.Empty],
            payload, ct);
        maintenance = new MaintenanceEvent
        {
            Id = eventId, AccountId = accountId, FirearmId = command.FirearmId, ComponentId = command.ComponentId,
            EventType = command.EventType, Date = command.Date, RoundCountAtEvent = derivedRoundCount,
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
        var next = NormalizeRecipe(snapshot) with { Version = recipe.Version + 1 };
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
        if (command.InitialRounds < 0) throw new WorkshopValidationException("Initial rounds cannot be negative.");
        var idempotencyKey = string.IsNullOrWhiteSpace(command.IdempotencyKey)
            ? $"firearm-created:{NewId()}"
            : command.IdempotencyKey.Trim();
        var existing = await repository.FindOperationAsync(accountId, idempotencyKey, ct);
        if (existing is not null)
        {
            var existingId = existing.SourceEntityIds.FirstOrDefault()
                ?? throw new WorkshopConflictException("The idempotent firearm operation has no source record.");
            return await repository.GetAsync<Firearm>(DbCollections.WorkshopFirearms, accountId, existingId, ct)
                ?? throw new WorkshopConflictException("The idempotent firearm could not be recovered.");
        }
        var firearm = new Firearm
        {
            Id = StableId(accountId, idempotencyKey, "firearm"), AccountId = accountId, Manufacturer = command.Manufacturer.Trim(),
            Model = command.Model.Trim(), Caliber = command.Caliber.Trim(), Category = Trim(command.Category),
            Nickname = Trim(command.Nickname), PurchaseDate = command.PurchaseDate, Notes = Trim(command.Notes),
            CreationDateTime = Now()
        };
        await repository.AddAsync(DbCollections.WorkshopFirearms, firearm, ct);
        await CommitAsync(accountId, idempotencyKey, WorkshopOperationType.FirearmCreated,
            firearm.CreationDateTime, [new(WorkshopAggregateKind.Firearm, firearm.Id, RoundsDelta: command.InitialRounds)],
            [firearm.Id], JsonSerializer.Serialize(command), ct);
        firearm = await repository.GetAsync<Firearm>(DbCollections.WorkshopFirearms, accountId, firearm.Id, ct) ?? firearm;
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
        RequireText(command.IdempotencyKey, "Idempotency key", 80);
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
            Id = NewId(), AccountId = accountId, Name = command.Name.Trim(),
            Manufacturer = Trim(command.Manufacturer), Model = Trim(command.Model),
            Description = Trim(command.Description), DefaultServiceInterval = command.DefaultServiceInterval,
            TrackingMode = command.TrackingMode, Notes = Trim(command.Notes),
            CompatibilityTags = NormalizeTags(command.CompatibilityTags), CreationDateTime = Now()
        };
        await repository.AddAsync(DbCollections.WorkshopComponentDefinitions, definition, ct);
        await RecordActivityAsync(accountId, WorkshopOperationType.ComponentDefinitionCreated, definition.Id, definition.Name, ct);
        return definition;
    }

    public async Task<ComponentInstance> CreateComponentAsync(string accountId, WorkshopComponentCommand command, CancellationToken ct = default)
    {
        if (command.InitialRounds < 0) throw new WorkshopValidationException("Initial rounds cannot be negative.");
        var definition = await repository.GetAsync<ComponentDefinition>(DbCollections.WorkshopComponentDefinitions, accountId, command.DefinitionId, ct)
            ?? throw new WorkshopValidationException("Component definition was not found.");
        var component = new ComponentInstance
        {
            Id = NewId(), AccountId = accountId, DefinitionId = command.DefinitionId,
            IdentificationNote = Trim(command.IdentificationNote), Manufacturer = Trim(command.Manufacturer),
            Model = Trim(command.Model), ServiceInterval = ValidateInterval(command.ServiceInterval, command.TrackingMode ?? WorkshopTrackingMode.Counter),
            TrackingMode = command.TrackingMode ?? WorkshopTrackingMode.Counter,
            CompatibilityTags = NormalizeTags(command.CompatibilityTags), Status = WorkshopComponentStatus.Inventory,
            Notes = Trim(command.Notes), CreationDateTime = Now()
        };
        await repository.AddAsync(DbCollections.WorkshopComponentInstances, component, ct);
        var idempotencyKey = string.IsNullOrWhiteSpace(command.IdempotencyKey) ? $"component-created:{component.Id}" : command.IdempotencyKey;
        await CommitAsync(accountId, idempotencyKey, WorkshopOperationType.ComponentCreated,
            component.CreationDateTime,
            [new(WorkshopAggregateKind.Component, component.Id, RoundsDelta: command.InitialRounds)],
            [component.Id], JsonSerializer.Serialize(command), ct);
        component = await repository.GetAsync<ComponentInstance>(
            DbCollections.WorkshopComponentInstances, accountId, component.Id, ct) ?? component;
        return component;
    }

    public async Task<ComponentDefinition> UpdateComponentDefinitionAsync(string accountId, string id,
        WorkshopUpdateComponentDefinitionCommand command, CancellationToken ct = default)
    {
        var current = await repository.GetAsync<ComponentDefinition>(DbCollections.WorkshopComponentDefinitions, accountId, id, ct)
            ?? throw new WorkshopValidationException("Component definition was not found.");
        RequireText(command.Name, "Name", 160);
        if (current.Revision != command.ExpectedRevision)
            throw new WorkshopConflictException("Component definition changed. Reload before updating.");
        var updated = current with { Name = command.Name.Trim(), Description = Trim(command.Description), Notes = Trim(command.Notes), Revision = current.Revision + 1 };
        if (!await repository.ReplaceAsync(DbCollections.WorkshopComponentDefinitions, updated, accountId, current.Revision, ct))
            throw new WorkshopConflictException("Component definition changed while updating it.");
        return updated;
    }

    public async Task<ComponentDefinition> ArchiveComponentDefinitionAsync(string accountId, string id,
        WorkshopArchiveComponentDefinitionCommand command, CancellationToken ct = default)
    {
        var current = await repository.GetAsync<ComponentDefinition>(DbCollections.WorkshopComponentDefinitions, accountId, id, ct)
            ?? throw new WorkshopValidationException("Component definition was not found.");
        if (current.Revision != command.ExpectedRevision)
            throw new WorkshopConflictException("Component definition changed. Reload before archiving.");
        return await repository.ReplaceAsync(DbCollections.WorkshopComponentDefinitions,
            current with { Archived = true, Revision = current.Revision + 1 }, accountId, current.Revision, ct)
            ? current with { Archived = true, Revision = current.Revision + 1 }
            : throw new WorkshopConflictException("Component definition changed while archiving it.");
    }

    public async Task<ComponentInstance> UpdateComponentAsync(string accountId, string id,
        WorkshopUpdateComponentCommand command, CancellationToken ct = default)
    {
        var current = await repository.GetAsync<ComponentInstance>(DbCollections.WorkshopComponentInstances, accountId, id, ct)
            ?? throw new WorkshopValidationException("Component was not found.");
        if (current.Revision != command.ExpectedRevision)
            throw new WorkshopConflictException("Component changed. Reload before updating.");
        if (command.TotalRounds is < 0)
            throw new WorkshopValidationException("Total rounds cannot be negative.");
        var roundDelta = command.TotalRounds is null ? 0 : command.TotalRounds.Value - current.TotalRounds;
        var updated = current with
        {
            IdentificationNote = Trim(command.IdentificationNote), Manufacturer = Trim(command.Manufacturer),
            Model = Trim(command.Model), ServiceInterval = ValidateInterval(command.ServiceInterval, command.TrackingMode),
            TrackingMode = command.TrackingMode, CompatibilityTags = NormalizeTags(command.CompatibilityTags),
            Notes = Trim(command.Notes), Revision = current.Revision + 1
        };
        if (roundDelta != 0)
        {
            var existing = await repository.FindOperationAsync(accountId, command.IdempotencyKey, ct);
            var payload = JsonSerializer.Serialize(command);
            var deltas = new[] { new WorkshopDelta(WorkshopAggregateKind.Component, id, roundDelta) };
            var sourceIds = new[] { id };
            var expectedHash = PayloadHash(WorkshopOperationType.ComponentUpdated, deltas, sourceIds, payload, null);
            if (existing is not null)
            {
                if (existing.PayloadHash != expectedHash)
                    throw new WorkshopConflictException("Idempotency key was already used for a different operation.");
                return current;
            }
            await CommitAsync(accountId, command.IdempotencyKey, WorkshopOperationType.ComponentUpdated,
                Now(), deltas, sourceIds, payload, ct);
            current = await repository.GetAsync<ComponentInstance>(
                DbCollections.WorkshopComponentInstances, accountId, id, ct)
                ?? throw new WorkshopConflictException("Component projection could not be recovered.");
            updated = updated with { TotalRounds = current.TotalRounds, Revision = current.Revision + 1 };
        }
        if (!await repository.ReplaceAsync(DbCollections.WorkshopComponentInstances, updated, accountId, current.Revision, ct))
            throw new WorkshopConflictException("Component changed while updating it.");
        return updated;
    }

    public async Task<ComponentInstance> ArchiveComponentAsync(string accountId, string id,
        WorkshopArchiveComponentCommand command, CancellationToken ct = default)
    {
        var current = await repository.GetAsync<ComponentInstance>(DbCollections.WorkshopComponentInstances, accountId, id, ct)
            ?? throw new WorkshopValidationException("Component was not found.");
        if (current.Status == WorkshopComponentStatus.Installed)
            throw new WorkshopValidationException("Remove the component before archiving it.");
        if (current.Revision != command.ExpectedRevision)
            throw new WorkshopConflictException("Component changed. Reload before archiving.");
        var updated = current with { Archived = true, Revision = current.Revision + 1 };
        if (!await repository.ReplaceAsync(DbCollections.WorkshopComponentInstances, updated, accountId, current.Revision, ct))
            throw new WorkshopConflictException("Component changed while archiving it.");
        return updated;
    }

    public async Task DeleteComponentDefinitionAsync(string accountId, string id, long revision, CancellationToken ct = default)
    {
        var current = await repository.GetAsync<ComponentDefinition>(DbCollections.WorkshopComponentDefinitions, accountId, id, ct)
            ?? throw new WorkshopValidationException("Component definition was not found.");
        if ((await repository.ListAsync<ComponentInstance>(DbCollections.WorkshopComponentInstances, accountId, ct)).Any(x => x.DefinitionId == id))
            throw new WorkshopConflictException("This definition is referenced by physical components. Archive it instead.");
        if (current.Revision != revision || !await repository.DeleteAsync<ComponentDefinition>(DbCollections.WorkshopComponentDefinitions, accountId, id, revision, ct))
            throw new WorkshopConflictException("Component definition changed. Reload before deleting it.");
    }

    public async Task DeleteComponentAsync(string accountId, string id, long revision, CancellationToken ct = default)
    {
        var current = await repository.GetAsync<ComponentInstance>(DbCollections.WorkshopComponentInstances, accountId, id, ct)
            ?? throw new WorkshopValidationException("Component was not found.");
        var installations = await repository.ListAsync<ComponentInstallation>(DbCollections.WorkshopComponentInstallations, accountId, ct);
        var maintenance = await repository.ListAsync<MaintenanceEvent>(DbCollections.WorkshopMaintenanceEvents, accountId, ct);
        var operations = await repository.ListAsync<WorkshopOperation>(DbCollections.WorkshopOperations, accountId, ct);
        if (installations.Any(x => x.ComponentId == id) || maintenance.Any(x => x.ComponentId == id) ||
            operations.Any(x => x.SourceEntityIds.Contains(id) || x.Deltas.Any(delta => delta.AggregateId == id)))
            throw new WorkshopConflictException("This component has history. Archive it instead.");
        if (current.Revision != revision || !await repository.DeleteAsync<ComponentInstance>(DbCollections.WorkshopComponentInstances, accountId, id, revision, ct))
            throw new WorkshopConflictException("Component changed. Reload before deleting it.");
    }

    public async Task<AmmoRecipe> CreateRecipeAsync(string accountId, RecipeSnapshot snapshot, CancellationToken ct = default)
    {
        RequireText(snapshot.Name, "Name", 160);
        RequireText(snapshot.Caliber, "Caliber", 80);
        snapshot = NormalizeRecipe(snapshot);
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
        await RecordActivityAsync(accountId, WorkshopOperationType.RecipeCreated, recipe.Id, recipe.Name, ct);
        return recipe;
    }

    public async Task<WorkshopOperationResult> RecordShootingAsync(string accountId, WorkshopShootingCommand command, CancellationToken ct = default)
    {
        if (command.TotalRounds <= 0) throw new WorkshopValidationException("Total rounds must be positive.");
        var firearm = await repository.GetAsync<Firearm>(DbCollections.WorkshopFirearms, accountId, command.FirearmId, ct)
            ?? throw new WorkshopValidationException("Firearm was not found.");
        if (firearm.Archived) throw new WorkshopValidationException("Archived firearms cannot receive shooting sessions.");
        var consumptionRecords = await ResolveConsumptionAsync(accountId, command.FirearmId, command.RecipeConsumptions, null, ct);
        var consumed = consumptionRecords.Sum(x => x.Quantity);
        if (consumed + command.UntrackedAmmunition != command.TotalRounds)
            throw new WorkshopValidationException("Known consumption plus untracked ammunition must equal total rounds.");
        var activeInstallations = (await repository.ListAsync<ComponentInstallation>(
                DbCollections.WorkshopComponentInstallations, accountId, ct))
            .Where(x => x.FirearmId == firearm.Id && x.InstalledAt <= command.Date &&
                        (x.RemovedAt is null || x.RemovedAt > command.Date))
            .ToArray();
        var knownBatchConsumption = consumptionRecords
            .GroupBy(x => x.BatchId!, StringComparer.Ordinal)
            .ToArray();

        var deltas = new List<WorkshopDelta> { new(WorkshopAggregateKind.Firearm, firearm.Id, RoundsDelta: command.TotalRounds) };
        foreach (var installation in activeInstallations)
        {
            _ = await repository.GetAsync<ComponentInstance>(
                DbCollections.WorkshopComponentInstances, accountId, installation.ComponentId, ct)
                ?? throw new WorkshopValidationException("A usage segment references an unknown component.");
            deltas.Add(new(WorkshopAggregateKind.Component, installation.ComponentId, RoundsDelta: command.TotalRounds,
                ComponentId: installation.ComponentId, InstallationId: installation.Id));
        }
        var derivedSegments = activeInstallations.Select(x => new ShootingUsageSegment(x.ComponentId, x.Id, command.TotalRounds)).ToArray();
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
                TotalRounds = command.TotalRounds, Notes = Trim(command.Notes),                 ConsumptionRecords = consumptionRecords,
                UsageSegments = derivedSegments, UntrackedAmmunition = command.UntrackedAmmunition,
                OperationId = operation.Operation.Id,
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
        if (recipe.Status != WorkshopRecipeStatus.Active)
            throw new WorkshopValidationException("Recipe was not found.");
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
        WorkshopReplacementResolution? replacement = command.Replacement is null
            ? null
            : await BuildReplacementDeltasAsync(accountId, command.Replacement, reversal, ct);
        IReadOnlyList<WorkshopDelta> replacementDeltas = replacement?.Deltas ?? [];
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
        {
            var replacementInstallations = await ActiveInstallationsAsync(accountId, command.Replacement.FirearmId,
                command.Replacement.Date, ct);
            await repository.AddAsync(DbCollections.WorkshopShootingSessions, new ShootingSession
            {
                Id = StableId(accountId, command.IdempotencyKey, "corrected-shooting"),
                AccountId = accountId, FirearmId = command.Replacement.FirearmId,
                Date = command.Replacement.Date, TotalRounds = command.Replacement.TotalRounds,
                Notes = Trim(command.Replacement.Notes),
                ConsumptionRecords = replacement!.ConsumptionRecords,
                UsageSegments = replacementInstallations.Select(x =>
                    new ShootingUsageSegment(x.ComponentId, x.Id, command.Replacement.TotalRounds)).ToArray(),
                UntrackedAmmunition = command.Replacement.UntrackedAmmunition,
                CorrectionOfId = original.Id, OperationId = result.Operation.Id, CreationDateTime = Now()
            }, ct);
        }
        return result;
    }

    public Task<WorkshopReconciliationResult> ReconcileAsync(string accountId, bool repair, CancellationToken ct = default) =>
        reconciliation.ReconcileAsync(accountId, repair, ct);

    private async Task<WorkshopReplacementResolution> BuildReplacementDeltasAsync(
        string accountId, WorkshopShootingCommand command, IReadOnlyList<WorkshopDelta> reversal,
        CancellationToken ct)
    {
        var consumptionRecords = await ResolveConsumptionAsync(accountId, command.FirearmId,
            command.RecipeConsumptions, reversal, ct);
        if (command.TotalRounds <= 0 ||
            consumptionRecords.Sum(x => x.Quantity) + command.UntrackedAmmunition != command.TotalRounds)
            throw new WorkshopValidationException("Replacement shooting quantities are inconsistent.");
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
        var activeInstallations = (await repository.ListAsync<ComponentInstallation>(
                DbCollections.WorkshopComponentInstallations, accountId, ct))
            .Where(x => x.FirearmId == command.FirearmId && x.InstalledAt <= command.Date &&
                        (x.RemovedAt is null || x.RemovedAt > command.Date))
            .ToArray();
        foreach (var installation in activeInstallations)
        {
            deltas.Add(new(WorkshopAggregateKind.Component, installation.ComponentId,
                RoundsDelta: command.TotalRounds, ComponentId: installation.ComponentId, InstallationId: installation.Id));
        }
        foreach (var batchGroup in consumptionRecords
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
        return new(deltas, consumptionRecords);
    }

    private async Task<IReadOnlyList<ShootingConsumption>> ResolveConsumptionAsync(
        string accountId, string firearmId, IReadOnlyList<ShootingRecipeConsumption> requested,
        IReadOnlyList<WorkshopDelta>? reversal, CancellationToken ct)
    {
        if (requested.Any(x => string.IsNullOrWhiteSpace(x.RecipeId) || x.Quantity <= 0))
            throw new WorkshopValidationException("Ammunition recipe quantities must be positive.");

        var batches = (await repository.ListAsync<AmmoBatch>(
                DbCollections.WorkshopAmmoBatches, accountId, ct))
            .Where(x => x.OnHandQuantity > 0)
            .OrderBy(x => x.ProductionDate)
            .ThenBy(x => x.Id, StringComparer.Ordinal)
            .ToArray();
        var replay = reversal is null
            ? null
            : WorkshopReplay.Rebuild((await repository.ListAsync<WorkshopOperation>(
                DbCollections.WorkshopOperations, accountId, ct)).Concat([new WorkshopOperation { Deltas = reversal }]));
        var resolved = new List<ShootingConsumption>();
        foreach (var request in requested
                     .GroupBy(x => x.RecipeId, StringComparer.Ordinal)
                     .Select(group => new ShootingRecipeConsumption(group.Key, group.Sum(x => x.Quantity))))
        {
            var remaining = request.Quantity;
            foreach (var batch in batches.Where(x => x.RecipeId == request.RecipeId))
            {
                var available = replay?.For(WorkshopAggregateKind.Batch, batch.Id).Stock ?? batch.OnHandQuantity;
                var amount = Math.Min(remaining, Math.Max(available, 0));
                if (amount <= 0) continue;
                resolved.Add(new(batch.Id, amount, null, request.RecipeId));
                remaining -= amount;
                if (remaining == 0) break;
            }
            if (remaining > 0)
                throw new WorkshopValidationException($"Insufficient ammunition for recipe '{request.RecipeId}'.");
        }
        return resolved;
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
        await RecordActivityAsync(accountId, type, operation.Id, await ActivityLabelAsync(accountId, type, operation.SourceEntityIds, ct), ct, operation);
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
        string? entityLabel, CancellationToken ct, WorkshopOperation? operation = null)
    {
        await repository.AddAsync(DbCollections.WorkshopActivities, new WorkshopActivity
        {
            Id = NewId(), AccountId = accountId, OperationId = operation?.Id ?? sourceId, Type = type,
            EffectiveAt = operation?.EffectiveAt ?? Now(), RecordedAt = operation?.RecordedAt ?? Now(),
            SourceEntityIds = operation?.SourceEntityIds ?? [sourceId],
            Summary = ActivitySummary(type), EntityLabel = entityLabel, CreationDateTime = Now()
        }, ct);
    }

    private async Task<string?> ActivityLabelAsync(string accountId, WorkshopOperationType type,
        IReadOnlyList<string> sourceIds, CancellationToken ct)
    {
        var ids = sourceIds.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        switch (type)
        {
            case WorkshopOperationType.FirearmCreated:
            case WorkshopOperationType.FirearmUpdated:
            case WorkshopOperationType.FirearmArchived:
            case WorkshopOperationType.ShootingRecorded:
                return await FirearmLabelAsync(accountId, ids.FirstOrDefault(), ct);
            case WorkshopOperationType.ShootingCorrected:
            case WorkshopOperationType.AllocationRecorded:
                foreach (var id in ids)
                {
                    var firearmLabel = await FirearmLabelAsync(accountId, id, ct);
                    if (firearmLabel is not null) return firearmLabel;
                }
                return null;
            case WorkshopOperationType.ComponentCreated:
            case WorkshopOperationType.ComponentUpdated:
            case WorkshopOperationType.ComponentArchived:
            case WorkshopOperationType.ComponentDeleted:
            case WorkshopOperationType.ComponentInstalled:
            case WorkshopOperationType.ComponentRemoved:
                return await ComponentLabelAsync(accountId, ids, ct);
            case WorkshopOperationType.MaintenanceRecorded:
                return await FirearmLabelAsync(accountId, ids.FirstOrDefault(), ct)
                    ?? await ComponentLabelAsync(accountId, ids, ct);
            case WorkshopOperationType.RecipeCreated:
            case WorkshopOperationType.RecipeUpdated:
            case WorkshopOperationType.RecipeArchived:
                return await RecipeLabelAsync(accountId, ids.FirstOrDefault(), ct);
            case WorkshopOperationType.ProductionRecorded:
                var batch = await repository.GetAsync<AmmoBatch>(
                    DbCollections.WorkshopAmmoBatches, accountId, ids.FirstOrDefault() ?? string.Empty, ct);
                return batch?.RecipeSnapshot.Name;
            case WorkshopOperationType.AllocationReleased:
            case WorkshopOperationType.AllocationReallocated:
                var allocation = await repository.GetAsync<AmmoAllocation>(
                    DbCollections.WorkshopAmmoAllocations, accountId, ids.FirstOrDefault() ?? string.Empty, ct);
                return allocation is null
                    ? null
                    : await FirearmLabelAsync(accountId, allocation.FirearmId, ct);
            default:
                return null;
        }
    }

    private async Task<string?> FirearmLabelAsync(string accountId, string? id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        var firearm = await repository.GetAsync<Firearm>(DbCollections.WorkshopFirearms, accountId, id, ct);
        return firearm is null ? null : FormatFirearmLabel(firearm);
    }

    private async Task<string?> ComponentLabelAsync(string accountId, IReadOnlyList<string> ids, CancellationToken ct)
    {
        foreach (var id in ids)
        {
            var component = await repository.GetAsync<ComponentInstance>(
                DbCollections.WorkshopComponentInstances, accountId, id, ct);
            if (component is null) continue;
            var definition = await repository.GetAsync<ComponentDefinition>(
                DbCollections.WorkshopComponentDefinitions, accountId, component.DefinitionId, ct);
            return FormatComponentLabel(component, definition);
        }
        return null;
    }

    private async Task<string?> RecipeLabelAsync(string accountId, string? id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        var recipe = await repository.GetAsync<AmmoRecipe>(DbCollections.WorkshopRecipes, accountId, id, ct);
        return recipe?.Name;
    }

    private static string FormatFirearmLabel(Firearm firearm) =>
        string.IsNullOrWhiteSpace(firearm.Nickname)
            ? $"{firearm.Manufacturer} {firearm.Model}".Trim()
            : firearm.Nickname;

    private static string FormatComponentLabel(ComponentInstance component, ComponentDefinition? definition)
    {
        var instance = $"{component.Manufacturer} {component.Model}".Trim();
        if (string.IsNullOrWhiteSpace(instance)) instance = component.IdentificationNote?.Trim() ?? string.Empty;
        if (definition is null || string.IsNullOrWhiteSpace(instance)) return definition?.Name ?? instance;
        return $"{definition.Name} — {instance}";
    }

    private DateTime Now() => timeProvider.GetUtcNow().UtcDateTime;
    private async Task<IReadOnlyList<ComponentInstallation>> ActiveInstallationsAsync(
        string accountId, string firearmId, DateTime at, CancellationToken ct) =>
        (await repository.ListAsync<ComponentInstallation>(
            DbCollections.WorkshopComponentInstallations, accountId, ct))
        .Where(x => x.FirearmId == firearmId && x.InstalledAt <= at &&
                    (x.RemovedAt is null || x.RemovedAt > at)).ToArray();
    private static string NewId() => ObjectId.GenerateNewId().ToString();
    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static RecipeSnapshot NormalizeRecipe(RecipeSnapshot snapshot)
    {
        var oal = snapshot.Oal;
        if (!string.IsNullOrWhiteSpace(oal))
        {
            oal = oal.Trim();
            if (!decimal.TryParse(oal, System.Globalization.NumberStyles.AllowLeadingSign |
                    System.Globalization.NumberStyles.AllowDecimalPoint,
                    System.Globalization.CultureInfo.InvariantCulture, out var value))
                throw new WorkshopValidationException("OAL must be a decimal value.");
            if (decimal.Round(value, 2) != value)
                throw new WorkshopValidationException("OAL must have at most two fractional digits.");
            oal = value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        }
        return snapshot with { Oal = oal };
    }
    private static IReadOnlyList<string> NormalizeTags(IEnumerable<string>? tags) =>
        (tags ?? []).Select(Trim).Where(x => x is not null).Select(x => x!.ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase).Take(20).Where(x => x.Length <= 80).ToArray();
    private static long? ValidateInterval(long? value, WorkshopTrackingMode mode)
    {
        if (mode == WorkshopTrackingMode.Interval && (!value.HasValue || value <= 0))
            throw new WorkshopValidationException("Maximum rounds is required for interval tracking.");
        if (mode != WorkshopTrackingMode.Interval && value.HasValue)
            throw new WorkshopValidationException("Maximum rounds is only valid for interval tracking.");
        return value;
    }
    private static string ActivitySummary(WorkshopOperationType type) => type switch
    {
        WorkshopOperationType.ComponentInstalled => "Component installed",
        WorkshopOperationType.ComponentRemoved => "Component removed",
        WorkshopOperationType.ShootingRecorded => "Shooting session recorded",
        WorkshopOperationType.MaintenanceRecorded => "Maintenance recorded",
        _ => type.ToString()
    };
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
