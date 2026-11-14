using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Domain.Workshop;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models.Workshop;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class WorkshopServiceTests
{
    [Fact]
    public async Task Firearm_edit_and_archive_use_revision_checks_and_are_idempotent()
    {
        var (service, scenario, firearm) = CreateService();

        var edited = await service.UpdateFirearmAsync("account-1", firearm.Id,
            new("Updated", "Model 2", "9mm", "Action", "New name", null, "notes", 0, "edit-1"));
        var archived = await service.ArchiveFirearmAsync("account-1", firearm.Id,
            new(edited.Revision, "archive-1"));

        var editRetry = await service.UpdateFirearmAsync("account-1", firearm.Id,
            new("Updated", "Model 2", "9mm", "Action", "New name", null, "notes", 0, "edit-1"));
        var archiveRetry = await service.ArchiveFirearmAsync("account-1", firearm.Id,
            new(edited.Revision, "archive-1"));

        Assert.Equal(edited.Id, editRetry.Id);
        Assert.Equal(archived.Id, archiveRetry.Id);
        Assert.True(scenario.Read<Firearm>(DbCollections.WorkshopFirearms).Single().Archived);
        Assert.Equal(2, scenario.Read<WorkshopOperation>(DbCollections.WorkshopOperations).Count);
    }

    [Fact]
    public async Task Release_and_reallocate_retries_return_the_original_operation()
    {
        var (service, scenario, firearm) = CreateService();
        var destination = firearm with { Id = "firearm-2", Nickname = "destination" };
        scenario.Add(DbCollections.WorkshopFirearms, destination);
        scenario.Add(DbCollections.WorkshopAmmoAllocations, new AmmoAllocation
        {
            Id = "allocation-1", AccountId = "account-1", BatchId = "batch-1", FirearmId = firearm.Id,
            Quantity = 100, CreationDateTime = DateTime.UtcNow
        });
        scenario.Add(DbCollections.WorkshopAmmoBatches, new AmmoBatch
        {
            Id = "batch-1", AccountId = "account-1", ProducedQuantity = 200, OnHandQuantity = 200,
            CreationDateTime = DateTime.UtcNow
        });
        scenario.Add(DbCollections.WorkshopOperations, new WorkshopOperation
        {
            Id = "allocation-op", AccountId = "account-1", ClientIdempotencyKey = "allocation-key",
            OperationType = WorkshopOperationType.AllocationRecorded,
            Deltas = [new(WorkshopAggregateKind.Allocation, "allocation-1", AllocationDelta: 100)],
            CreationDateTime = DateTime.UtcNow
        });

        var released = await service.ReleaseAllocationAsync("account-1",
            new("allocation-1", 25, 0, "release-1"));
        var releaseRetry = await service.ReleaseAllocationAsync("account-1",
            new("allocation-1", 25, 0, "release-1"));
        var moved = await service.ReallocateAsync("account-1",
            new("allocation-1", destination.Id, 25, "move-1"));
        var moveRetry = await service.ReallocateAsync("account-1",
            new("allocation-1", destination.Id, 25, "move-1"));

        Assert.True(releaseRetry.Idempotent);
        Assert.True(moveRetry.Idempotent);
        Assert.NotEqual(released.Operation.Id, moved.Operation.Id);
        Assert.Equal(50, scenario.Read<AmmoAllocation>(DbCollections.WorkshopAmmoAllocations)
            .Single(x => x.Id == "allocation-1").ReleasedQuantity);
    }

    [Fact]
    public async Task Shooting_correction_retry_is_idempotent_after_the_original_is_corrected()
    {
        var (service, scenario, firearm) = CreateService();
        scenario.Add(DbCollections.WorkshopOperations, new WorkshopOperation
        {
            Id = "shoot-1", AccountId = "account-1", ClientIdempotencyKey = "shoot-key",
            OperationType = WorkshopOperationType.ShootingRecorded,
            Deltas = [new(WorkshopAggregateKind.Firearm, firearm.Id, RoundsDelta: 20)],
            CreationDateTime = DateTime.UtcNow
        });

        var command = new WorkshopCorrectionCommand("shoot-1", null, "correct-1");
        var first = await service.CorrectShootingAsync("account-1", command);
        var retry = await service.CorrectShootingAsync("account-1", command);

        Assert.False(first.Idempotent);
        Assert.True(retry.Idempotent);
        Assert.Equal(first.Operation.Id, retry.Operation.Id);
    }

    [Fact]
    public async Task Shooting_derives_rounds_for_every_component_active_at_session_time()
    {
        var (service, scenario, firearm) = CreateService();
        scenario.Add(DbCollections.WorkshopComponentDefinitions, new ComponentDefinition
        {
            Id = "definition-1", AccountId = "account-1", Name = "Optic"
        });
        foreach (var id in new[] { "component-1", "component-2" })
        {
            scenario.Add(DbCollections.WorkshopComponentInstances, new ComponentInstance
            {
                Id = id, AccountId = "account-1", DefinitionId = "definition-1"
            });
            scenario.Add(DbCollections.WorkshopComponentInstallations, new ComponentInstallation
            {
                Id = $"installation-{id}", AccountId = "account-1", ComponentId = id,
                FirearmId = firearm.Id, InstalledAt = DateTime.UtcNow.AddDays(-1)
            });
        }

        var result = await service.RecordShootingAsync("account-1",
            new(firearm.Id, DateTime.UtcNow, 40, null, [], 40, [], "shoot-derived"));

        Assert.Equal(40, result.Operation.Deltas.Single(x => x.AggregateKind == WorkshopAggregateKind.Firearm).RoundsDelta);
        Assert.Equal(2, result.Operation.Deltas.Count(x => x.AggregateKind == WorkshopAggregateKind.Component));
        Assert.All(result.Operation.Deltas.Where(x => x.AggregateKind == WorkshopAggregateKind.Component),
            delta => Assert.Equal(40, delta.RoundsDelta));
    }

    [Fact]
    public async Task Create_and_install_is_recoverable_and_normalizes_definition_tags()
    {
        var (service, scenario, firearm) = CreateService();
        var definition = await service.CreateComponentDefinitionAsync("account-1",
            new("Sight", null, null, null, null, WorkshopTrackingMode.Counter, null, [" CZ ", "cz", "Tanfoglio"]));

        var command = new WorkshopCreateAndInstallCommand(definition.Id, firearm.Id, "serial", null, null,
            null, null, null, DateTime.UtcNow, null, "create-install-1");
        var first = await service.CreateAndInstallComponentAsync("account-1", command);
        var retry = await service.CreateAndInstallComponentAsync("account-1", command);

        Assert.Equal(first.Component.Id, retry.Component.Id);
        Assert.Equal(first.Installation.Id, retry.Installation.Id);
        Assert.Equal(["cz", "tanfoglio"], scenario.Read<ComponentDefinition>(
            DbCollections.WorkshopComponentDefinitions).Single().CompatibilityTags);
        Assert.Single(scenario.Read<ComponentInstallation>(DbCollections.WorkshopComponentInstallations));
    }

    [Fact]
    public async Task Recipe_writes_canonicalize_oal_and_reject_precision_beyond_two_digits()
    {
        var (service, _, _) = CreateService();

        var recipe = await service.CreateRecipeAsync("account-1",
            new("9mm", "9x19", "1.230", null, null, null, null, null, null, null));

        Assert.Equal("1.23", recipe.Oal);
        await Assert.ThrowsAsync<WorkshopValidationException>(() => service.CreateRecipeAsync("account-1",
            new("invalid", "9x19", "1.234", null, null, null, null, null, null, null)));
    }

    [Fact]
    public async Task Initial_firearm_rounds_are_recorded_in_the_ledger_projection()
    {
        var (service, scenario, _) = CreateService();

        var firearm = await service.CreateFirearmAsync("account-1",
            new("Maker", "New", "9mm", null, null, null, null, 125));

        Assert.Equal(125, firearm.TotalRounds);
        var stored = scenario.Read<Firearm>(DbCollections.WorkshopFirearms).Single(x => x.Id == firearm.Id);
        Assert.Equal(125, stored.TotalRounds);
        var operation = scenario.Read<WorkshopOperation>(DbCollections.WorkshopOperations).Single();
        Assert.Equal(125, operation.Deltas.Single().RoundsDelta);
        Assert.Equal(WorkshopOperationType.FirearmCreated, operation.OperationType);
    }

    private static (WorkshopService Service, FakeScenario Scenario, Firearm Firearm) CreateService()
    {
        var scenario = new FakeScenario();
        var firearm = new Firearm
        {
            Id = "firearm-1", AccountId = "account-1", Manufacturer = "Maker", Model = "Model",
            Caliber = "9mm", CreationDateTime = DateTime.UtcNow
        };
        scenario.Add(DbCollections.WorkshopFirearms, firearm);
        var repository = new WorkshopMockRepository(scenario);
        return (new WorkshopService(repository, new WorkshopReconciliationService(repository, TimeProvider.System),
            TimeProvider.System), scenario, firearm);
    }

    private sealed class FakeScenario : IMockScenario
    {
        private readonly Dictionary<string, List<BaseEntity>> collections = new();

        public void Reset() => collections.Clear();
        public IList<T> Read<T>(string collectionName) where T : BaseEntity =>
            collections.TryGetValue(collectionName, out var values)
                ? values.Cast<T>().ToList()
                : [];
        public T Add<T>(string collectionName, T item) where T : BaseEntity
        {
            if (!collections.TryGetValue(collectionName, out var values))
                collections[collectionName] = values = [];
            values.Add(item);
            return item;
        }
        public void Replace<T>(string collectionName, T item) where T : BaseEntity
        {
            var values = collections[collectionName];
            values[values.FindIndex(x => x.Id == item.Id)] = item;
        }
        public void Delete<T>(string collectionName, string id) where T : BaseEntity =>
            collections[collectionName].RemoveAll(x => x.Id == id);
    }
}
