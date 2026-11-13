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
