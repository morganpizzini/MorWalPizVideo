using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Domain.Workshop;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models.Workshop;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class WorkshopServiceTests
{
    [Fact]
    public async Task Activities_store_human_readable_entity_labels_instead_of_ids()
    {
        var (service, scenario, _) = CreateService();

        await service.CreateComponentDefinitionAsync("account-1",
            new("Competition trigger", "Timney", "Alpha", null, 5000, WorkshopTrackingMode.Interval, null));

        var activity = scenario.Read<WorkshopActivity>(DbCollections.WorkshopActivities).Single();

        Assert.Equal("Competition trigger", activity.EntityLabel);
        Assert.DoesNotContain(activity.EntityLabel, activity.SourceEntityIds);
    }

    [Fact]
    public async Task Component_definition_create_retry_returns_the_same_item_and_rejects_changed_payload()
    {
        var (service, scenario, _) = CreateService();
        var command = new WorkshopComponentDefinitionCommand(
            "Competition trigger", "Timney", "Alpha", null, 5000,
            WorkshopTrackingMode.Interval, null, IdempotencyKey: "definition-create-1");

        var created = await service.CreateComponentDefinitionAsync("account-1", command);
        var retry = await service.CreateComponentDefinitionAsync("account-1", command);

        Assert.Equal(created.Id, retry.Id);
        Assert.Single(scenario.Read<ComponentDefinition>(DbCollections.WorkshopComponentDefinitions));
        Assert.Single(scenario.Read<WorkshopOperation>(DbCollections.WorkshopOperations));
        await Assert.ThrowsAsync<WorkshopConflictException>(() =>
            service.CreateComponentDefinitionAsync("account-1", command with { Name = "Different trigger" }));
    }

    [Fact]
    public async Task Component_definition_archive_retry_returns_archived_item()
    {
        var (service, scenario, _) = CreateService();
        var definition = await service.CreateComponentDefinitionAsync("account-1",
            new("Optic", null, null, null, null, WorkshopTrackingMode.Manual, null,
                IdempotencyKey: "definition-create-archive"));
        var command = new WorkshopArchiveComponentDefinitionCommand(0, "definition-archive-1");

        var archived = await service.ArchiveComponentDefinitionAsync("account-1", definition.Id, command);
        var retry = await service.ArchiveComponentDefinitionAsync("account-1", definition.Id, command);

        Assert.True(archived.Archived);
        Assert.Equal(archived.Revision, retry.Revision);
        Assert.Equal(2, scenario.Read<WorkshopOperation>(DbCollections.WorkshopOperations).Count);
    }

    [Fact]
    public async Task Shooting_retry_succeeds_after_its_ammunition_has_been_consumed()
    {
        var (service, scenario, firearm) = CreateService();
        scenario.Add(DbCollections.WorkshopRecipes, new AmmoRecipe
        {
            Id = "recipe-1", AccountId = "account-1", Name = "Practice", Caliber = firearm.Caliber,
            Status = WorkshopRecipeStatus.Active
        });
        scenario.Add(DbCollections.WorkshopAmmoBatches, new AmmoBatch
        {
            Id = "batch-1", AccountId = "account-1", RecipeId = "recipe-1",
            ProductionDate = DateTime.UtcNow.AddDays(-1), ProducedQuantity = 20, OnHandQuantity = 20
        });
        var command = new WorkshopShootingCommand(
            firearm.Id, DateTime.UtcNow, 20, null, [new("recipe-1", 20)], 0, [], "shoot-retry-1");

        var first = await service.RecordShootingAsync("account-1", command);
        var retry = await service.RecordShootingAsync("account-1", command);

        Assert.False(first.Idempotent);
        Assert.True(retry.Idempotent);
        Assert.Equal(first.Operation.Id, retry.Operation.Id);
        Assert.Single(scenario.Read<ShootingSession>(DbCollections.WorkshopShootingSessions));
        Assert.Single(scenario.Read<WorkshopOperation>(DbCollections.WorkshopOperations));
    }

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
    public async Task Firearm_detail_returns_paginated_installation_history_for_the_requested_firearm()
    {
        var (service, scenario, firearm) = CreateService();
        scenario.Add(DbCollections.WorkshopComponentDefinitions, new ComponentDefinition
        {
            Id = "definition-1", AccountId = "account-1", Name = "Optic"
        });
        foreach (var id in new[] { "component-current", "component-previous", "component-other" })
        {
            scenario.Add(DbCollections.WorkshopComponentInstances, new ComponentInstance
            {
                Id = id, AccountId = "account-1", DefinitionId = "definition-1",
                IdentificationNote = id
            });
        }
        scenario.Add(DbCollections.WorkshopFirearms, firearm with { Id = "firearm-2" });
        scenario.Add(DbCollections.WorkshopComponentInstallations, new ComponentInstallation
        {
            Id = "installation-previous", AccountId = "account-1", ComponentId = "component-previous",
            FirearmId = firearm.Id, InstalledAt = DateTime.UtcNow.AddDays(-10),
            RemovedAt = DateTime.UtcNow.AddDays(-5)
        });
        scenario.Add(DbCollections.WorkshopComponentInstallations, new ComponentInstallation
        {
            Id = "installation-current", AccountId = "account-1", ComponentId = "component-current",
            FirearmId = firearm.Id, InstalledAt = DateTime.UtcNow.AddDays(-2)
        });
        scenario.Add(DbCollections.WorkshopComponentInstallations, new ComponentInstallation
        {
            Id = "installation-other", AccountId = "account-1", ComponentId = "component-other",
            FirearmId = "firearm-2", InstalledAt = DateTime.UtcNow.AddDays(-1)
        });

        var firstPage = await service.GetFirearmDetailAsync("account-1", firearm.Id,
            installationPage: 1, pageSize: 1);
        var secondPage = await service.GetFirearmDetailAsync("account-1", firearm.Id,
            installationPage: 2, pageSize: 1);

        Assert.NotNull(firstPage);
        Assert.Equal("installation-current", firstPage.InstallationHistory.Single().Installation.Id);
        Assert.True(firstPage.InstallationHistoryHasMore);
        Assert.NotNull(secondPage);
        Assert.Equal("installation-previous", secondPage.InstallationHistory.Single().Installation.Id);
        Assert.False(secondPage.InstallationHistoryHasMore);
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
    public async Task Shooting_consumes_selected_recipe_batches_oldest_first()
    {
        var (service, scenario, firearm) = CreateService();
        scenario.Add(DbCollections.WorkshopRecipes, new AmmoRecipe
        {
            Id = "recipe-1", AccountId = "account-1", Name = "Practice", Caliber = "9mm"
        });
        scenario.Add(DbCollections.WorkshopAmmoBatches, new AmmoBatch
        {
            Id = "newer", AccountId = "account-1", RecipeId = "recipe-1",
            ProductionDate = DateTime.UtcNow.AddDays(-1), ProducedQuantity = 100, OnHandQuantity = 100
        });
        scenario.Add(DbCollections.WorkshopAmmoBatches, new AmmoBatch
        {
            Id = "older", AccountId = "account-1", RecipeId = "recipe-1",
            ProductionDate = DateTime.UtcNow.AddDays(-2), ProducedQuantity = 25, OnHandQuantity = 25
        });

        var result = await service.RecordShootingAsync("account-1",
            new(firearm.Id, DateTime.UtcNow, 40, null,
                [new ShootingRecipeConsumption("recipe-1", 40)], 0, [], "shoot-fifo"));

        var consumption = scenario.Read<ShootingSession>(DbCollections.WorkshopShootingSessions).Single().ConsumptionRecords;
        Assert.Equal([("older", 25L), ("newer", 15L)],
            consumption.Select(x => (x.BatchId, x.Quantity)).ToArray());
        Assert.Equal(result.Operation.Id, scenario.Read<ShootingSession>(
            DbCollections.WorkshopShootingSessions).Single().OperationId);
        Assert.Equal(-25, result.Operation.Deltas.Single(x => x.AggregateId == "older").StockDelta);
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

    [Fact]
    public async Task Initial_component_rounds_are_recorded_in_the_ledger_projection()
    {
        var (service, scenario, firearm) = CreateService();
        var definition = await service.CreateComponentDefinitionAsync("account-1",
            new("Barrel", null, null, null, null, WorkshopTrackingMode.Counter, null));
        var component = await service.CreateAndInstallComponentAsync("account-1",
            new(definition.Id, firearm.Id, null, null, null, null, WorkshopTrackingMode.Counter,
                null, DateTime.UtcNow, null, "component-initial-1", InitialRounds: 35));

        Assert.Equal(35, component.Component.TotalRounds);
        var operation = scenario.Read<WorkshopOperation>(DbCollections.WorkshopOperations).Single(x =>
            x.Deltas.Any(delta => delta.AggregateId == component.Component.Id && delta.RoundsDelta == 35));
        Assert.Equal(35, operation.Deltas.Single(delta => delta.AggregateId == component.Component.Id).RoundsDelta);
    }

    [Fact]
    public async Task Component_round_correction_appends_a_delta_and_updates_the_projection()
    {
        var (service, scenario, firearm) = CreateService();
        var definition = await service.CreateComponentDefinitionAsync("account-1",
            new("Barrel", null, null, null, null, WorkshopTrackingMode.Counter, null));
        var created = await service.CreateAndInstallComponentAsync("account-1",
            new(definition.Id, firearm.Id, null, null, null, null, WorkshopTrackingMode.Counter,
                null, DateTime.UtcNow, null, "component-correction-create", InitialRounds: 10));

        var updated = await service.UpdateComponentAsync("account-1", created.Component.Id,
            new(null, null, null, null, WorkshopTrackingMode.Counter, null, null,
                created.Component.Revision, "component-correction-update", 25));

        Assert.Equal(25, updated.TotalRounds);
        Assert.Contains(scenario.Read<WorkshopOperation>(DbCollections.WorkshopOperations),
            operation => operation.Deltas.Any(delta => delta.AggregateId == created.Component.Id && delta.RoundsDelta == 15));
    }

    [Fact]
    public async Task Firearm_creation_retry_returns_the_original_projection()
    {
        var (service, scenario, _) = CreateService();
        var command = new WorkshopFirearmCommand("Maker", "Retry", "9mm", null, null, null, null, 25, "create-firearm-1");

        var first = await service.CreateFirearmAsync("account-1", command);
        var retry = await service.CreateFirearmAsync("account-1", command);

        Assert.Equal(first.Id, retry.Id);
        Assert.Single(scenario.Read<WorkshopOperation>(DbCollections.WorkshopOperations));
        Assert.Single(scenario.Read<Firearm>(DbCollections.WorkshopFirearms), x => x.Id == first.Id);
    }

    [Fact]
    public async Task Analytics_reports_tracked_and_untracked_rounds_for_the_requested_period()
    {
        var (service, scenario, firearm) = CreateService();
        var date = DateTime.UtcNow.AddDays(-2);
        scenario.Add(DbCollections.WorkshopRecipes, new AmmoRecipe
        {
            Id = "analytics-recipe", AccountId = "account-1", Name = "Practice", Caliber = "9mm"
        });
        scenario.Add(DbCollections.WorkshopAmmoBatches, new AmmoBatch
        {
            Id = "analytics-batch", AccountId = "account-1", RecipeId = "analytics-recipe",
            ProductionDate = date, ProducedQuantity = 100, OnHandQuantity = 100, Notes = "fresh batch"
        });
        await service.RecordShootingAsync("account-1",
            new(firearm.Id, date, 40, "practice session", [new("analytics-recipe", 30)], 10, [], "analytics-shoot-1"));

        var analytics = await service.GetAnalyticsAsync("account-1", date.AddDays(-1), date.AddDays(1));

        Assert.Equal(40, analytics.ShootingRounds);
        Assert.Equal(30, analytics.TrackedRounds);
        Assert.Equal(10, analytics.UntrackedRounds);
        Assert.Equal(1, analytics.ShootingSessions);
        var bucket = Assert.Single(analytics.Buckets);
        Assert.Equal(40, bucket.ShootingRounds);
        Assert.Equal(100, bucket.ProducedRounds);
        Assert.Equal(1, bucket.ShootingSessions);
        Assert.Equal(["practice session", "fresh batch"], bucket.Notes);
    }

    [Fact]
    public async Task Analytics_rejects_periods_longer_than_one_year()
    {
        var (service, _, _) = CreateService();

        await Assert.ThrowsAsync<WorkshopValidationException>(() =>
            service.GetAnalyticsAsync("account-1", DateTime.UtcNow.AddDays(-367), DateTime.UtcNow));
    }

    [Fact]
    public async Task Analytics_excludes_a_session_corrected_after_the_selected_period()
    {
        var (service, _, firearm) = CreateService();
        var date = DateTime.UtcNow.AddDays(-2);
        var shooting = await service.RecordShootingAsync("account-1",
            new(firearm.Id, date, 20, null, [], 20, [], "analytics-correction-shoot"));

        await service.CorrectShootingAsync("account-1",
            new(shooting.Operation.Id, null, "analytics-correction"));

        var analytics = await service.GetAnalyticsAsync("account-1", date.AddDays(-1), date.AddDays(1));

        Assert.Equal(0, analytics.ShootingRounds);
        Assert.Equal(0, analytics.ShootingSessions);
        Assert.Equal(0, analytics.CorrectionCount);
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
