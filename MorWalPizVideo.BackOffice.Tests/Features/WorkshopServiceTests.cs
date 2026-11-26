using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MorWalPiz.Contracts.Contracts.Workshop;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Domain.Workshop;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models.Workshop;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class WorkshopServiceTests
{
    [Fact]
    public void Stored_drawing_fields_are_tolerated_and_preserved_without_exposing_drawing_contracts()
    {
        var id = ObjectId.GenerateNewId().ToString();
        var snapshot = new RecipeSnapshot("Practice", "9mm", null, null, null, null, null, null, null);

        AssertDrawingRoundTrips(new AmmoRecipe
        {
            Id = id, AccountId = "account-1", Name = "Practice", Caliber = "9mm"
        });
        AssertDrawingRoundTrips(new AmmoRecipeVersion
        {
            Id = id, AccountId = "account-1", RecipeId = id, Version = 1, Snapshot = snapshot
        }, "snapshot");
        AssertDrawingRoundTrips(new ProductionSession
        {
            Id = id, AccountId = "account-1", RecipeId = id, RecipeSnapshot = snapshot
        }, "recipeSnapshot");
        AssertDrawingRoundTrips(new AmmoBatch
        {
            Id = id, AccountId = "account-1", RecipeId = id, RecipeSnapshot = snapshot
        }, "recipeSnapshot");

        var request = JsonSerializer.Deserialize<WorkshopRecipeRequest>(
            """{"name":"Practice","caliber":"9mm","drawing":"legacy drawing"}""",
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(request);
        Assert.Equal("Practice", request.Name);
        Assert.Null(typeof(AmmoRecipe).GetProperty("Drawing"));
        Assert.Null(typeof(RecipeSnapshot).GetProperty("Drawing"));
        Assert.Null(typeof(WorkshopRecipeRequest).GetProperty("Drawing"));
        Assert.DoesNotContain("drawing",
            JsonSerializer.Serialize(request, new JsonSerializerOptions(JsonSerializerDefaults.Web)).ToLowerInvariant());

        static void AssertDrawingRoundTrips<T>(T entity, string? snapshotElement = null) where T : BaseEntity
        {
            var storedDocument = entity.ToBsonDocument();
            if (snapshotElement is null)
                storedDocument["drawing"] = "legacy drawing";
            else
                storedDocument[snapshotElement].AsBsonDocument["drawing"] = "legacy drawing";

            var restoredEntity = BsonSerializer.Deserialize<T>(storedDocument);
            var restored = restoredEntity.ToBsonDocument();
            var retainedDrawing = snapshotElement is null
                ? restored["drawing"]
                : restored[snapshotElement].AsBsonDocument["drawing"];

            Assert.Equal("legacy drawing", retainedDrawing.AsString);
            Assert.DoesNotContain("drawing",
                JsonSerializer.Serialize(restoredEntity, new JsonSerializerOptions(JsonSerializerDefaults.Web))
                    .ToLowerInvariant());
        }
    }

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
        var primer = await service.CreateSupplyProductAsync("account-1", "primer", null, "Primer", "recipe-primer");
        var powder = await service.CreateSupplyProductAsync("account-1", "powder", null, "Powder", "recipe-powder");

        var recipe = await service.CreateRecipeAsync("account-1",
            new("9mm", "9x19", "1.230", null, null, null, null, null, null, primer.Id, powder.Id));

        Assert.Equal("1.23", recipe.Oal);
        await Assert.ThrowsAsync<WorkshopValidationException>(() => service.CreateRecipeAsync("account-1",
            new("invalid", "9x19", "1.234", null, null, null, null, null, null, null)));
    }

    [Fact]
    public async Task Recipe_journal_retries_are_idempotent_and_entries_remain_readable_after_archive()
    {
        var (service, scenario, _) = CreateService();
        scenario.Add(DbCollections.WorkshopRecipes, new AmmoRecipe
        {
            Id = "recipe-1",
            AccountId = "account-1",
            Name = "Practice",
            Caliber = "9x19",
            Status = WorkshopRecipeStatus.Active
        });
        var command = new WorkshopRecipeJournalCommand(
            " 2026-11-25 ", "Morning", " Clear ", "-2.50", "c", "123.40", " range note ", "journal-key");

        var first = await service.CreateRecipeJournalEntryAsync("account-1", "recipe-1", command);
        var retry = await service.CreateRecipeJournalEntryAsync("account-1", "recipe-1", command);

        Assert.Equal(first.Id, retry.Id);
        Assert.Equal("2026-11-25", first.EntryDate);
        Assert.Equal("Clear", first.Weather);
        Assert.Equal("-2.50", first.Temperature);
        Assert.Equal("C", first.TemperatureUnit);
        Assert.Equal("123.40", first.PowerFactor);
        Assert.Equal("range note", first.Notes);
        Assert.Single(scenario.Read<WorkshopRecipeJournalEntry>(DbCollections.WorkshopRecipeJournalEntries));
        Assert.Null(await service.GetRecipeJournalEntriesAsync("account-2", "recipe-1"));

        await Assert.ThrowsAsync<WorkshopConflictException>(() => service.CreateRecipeJournalEntryAsync(
            "account-1", "recipe-1", command with { Notes = "changed payload" }));

        scenario.Replace(DbCollections.WorkshopRecipes, new AmmoRecipe
        {
            Id = "recipe-1",
            AccountId = "account-1",
            Name = "Practice",
            Caliber = "9x19",
            Status = WorkshopRecipeStatus.Archived
        });

        var archivedEntries = await service.GetRecipeJournalEntriesAsync("account-1", "recipe-1");
        Assert.Equal(first.Id, Assert.Single(archivedEntries!).Id);
        Assert.Equal(first.Id, (await service.CreateRecipeJournalEntryAsync(
            "account-1", "recipe-1", command)).Id);
        await Assert.ThrowsAsync<WorkshopConflictException>(() => service.CreateRecipeJournalEntryAsync(
            "account-1", "recipe-1", command with { IdempotencyKey = "new-journal-key" }));
    }

    [Fact]
    public async Task Recipe_journal_validates_calendar_date_required_details_and_decimal_fields()
    {
        var (service, scenario, _) = CreateService();
        scenario.Add(DbCollections.WorkshopRecipes, new AmmoRecipe
        {
            Id = "recipe-1",
            AccountId = "account-1",
            Name = "Practice",
            Caliber = "9x19",
            Status = WorkshopRecipeStatus.Active
        });

        await Assert.ThrowsAsync<WorkshopValidationException>(() => service.CreateRecipeJournalEntryAsync(
            "account-1", "recipe-1", new("2026-02-29", null, null, null, null, null, null, "invalid-date")));
        await Assert.ThrowsAsync<WorkshopValidationException>(() => service.CreateRecipeJournalEntryAsync(
            "account-1", "recipe-1", new("2026-11-25", null, null, null, null, null, null, "empty-entry")));
        await Assert.ThrowsAsync<WorkshopValidationException>(() => service.CreateRecipeJournalEntryAsync(
            "account-1", "recipe-1", new("2026-11-25", null, null, "-2", null, null, null, "missing-unit")));
        await Assert.ThrowsAsync<WorkshopValidationException>(() => service.CreateRecipeJournalEntryAsync(
            "account-1", "recipe-1", new("2026-11-25", null, null, null, null, "not-a-number", null, "invalid-decimal")));
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

    [Fact]
    public async Task Production_edits_reverse_and_replace_batch_and_fifo_lot_effects_without_erasing_history()
    {
        var (service, scenario, firearm) = CreateService();
        var primer = await service.CreateSupplyProductAsync("account-1", "primer", null, "Primer A", "primer-product");
        var bullet = await service.CreateSupplyProductAsync("account-1", "bullet", "Brand", "Bullet A", "bullet-product");
        var powder = await service.CreateSupplyProductAsync("account-1", "powder", null, "Powder A", "powder-product");
        var primerPurchase = await service.RecordSupplyPurchaseAsync("account-1", primer.Id,
            DateTime.UtcNow.AddDays(-4), 300, 30m, "USD", "primer-buy");
        var firstBulletPurchase = await service.RecordSupplyPurchaseAsync("account-1", bullet.Id,
            DateTime.UtcNow.AddDays(-3), 100, 100m, "USD", "bullet-buy-1");
        var secondBulletPurchase = await service.RecordSupplyPurchaseAsync("account-1", bullet.Id,
            DateTime.UtcNow.AddDays(-2), 100, 200m, "USD", "bullet-buy-2");
        await service.RecordSupplyPurchaseAsync("account-1", powder.Id,
            DateTime.UtcNow.AddDays(-1), 1, 10m, "USD", "powder-buy");
        var recipe = await service.CreateRecipeAsync("account-1",
            new("Practice", "9mm", null, null, null, null, null, null, null, primer.Id, powder.Id));

        var created = await service.RecordProductionWithSuppliesAsync("account-1",
            new(recipe.Id, DateTime.UtcNow.AddHours(-2), 50, null, "production-create", recipe.Version,
                bullet.Id, [new(primerPurchase.Lots.Single().Id, 50)],
                [new(firstBulletPurchase.Lots.Single().Id, 50)]));

        var increased = await service.UpdateProductionAsync("account-1", created.Session.Id,
            new(new(recipe.Id, DateTime.UtcNow.AddHours(-1), 120, "corrected", "production-edit-1",
                    recipe.Version, bullet.Id, [new(primerPurchase.Lots.Single().Id, 120)],
                    [new(firstBulletPurchase.Lots.Single().Id, 100), new(secondBulletPurchase.Lots.Single().Id, 20)]),
                created.Session.Revision));

        Assert.Equal(120, increased.Session.QuantityProduced);
        Assert.Equal(120, increased.Batch.OnHandQuantity);
        Assert.Equal(180, scenario.Read<WorkshopSupplyLot>(DbCollections.WorkshopSupplyLots)
            .Single(x => x.Id == primerPurchase.Lots.Single().Id).AvailableQuantity);
        Assert.Equal(0, scenario.Read<WorkshopSupplyLot>(DbCollections.WorkshopSupplyLots)
            .Single(x => x.Id == firstBulletPurchase.Lots.Single().Id).AvailableQuantity);
        Assert.Equal(80, scenario.Read<WorkshopSupplyLot>(DbCollections.WorkshopSupplyLots)
            .Single(x => x.Id == secondBulletPurchase.Lots.Single().Id).AvailableQuantity);

        var decreased = await service.UpdateProductionAsync("account-1", created.Session.Id,
            new(new(recipe.Id, DateTime.UtcNow, 75, "smaller correction", "production-edit-2",
                    recipe.Version, bullet.Id, [new(primerPurchase.Lots.Single().Id, 75)],
                    [new(firstBulletPurchase.Lots.Single().Id, 75)]),
                increased.Session.Revision));
        Assert.Equal(75, decreased.Session.QuantityProduced);
        Assert.Equal(75, decreased.Batch.OnHandQuantity);
        Assert.Equal(25, scenario.Read<WorkshopSupplyLot>(DbCollections.WorkshopSupplyLots)
            .Single(x => x.Id == firstBulletPurchase.Lots.Single().Id).AvailableQuantity);
        Assert.Equal(100, scenario.Read<WorkshopSupplyLot>(DbCollections.WorkshopSupplyLots)
            .Single(x => x.Id == secondBulletPurchase.Lots.Single().Id).AvailableQuantity);
        Assert.Equal(3, scenario.Read<WorkshopOperation>(DbCollections.WorkshopOperations)
            .Count(x => x.OperationType is WorkshopOperationType.ProductionRecorded or WorkshopOperationType.ProductionEdited));
        var records = await service.GetProductionRecordsAsync("account-1");
        var record = Assert.Single(records);
        Assert.False(record.ProjectionPending);
        Assert.Equal(2, record.Corrections.Count);
        Assert.Equal(50, record.Corrections[0].Correction.PreviousQuantity);
        Assert.Equal(120, record.Corrections[0].Correction.Quantity);
        Assert.Equal(120, record.Corrections[1].Correction.PreviousQuantity);
        Assert.Equal(75, record.Corrections[1].Correction.Quantity);

        await service.RecordShootingAsync("account-1",
            new(firearm.Id, DateTime.UtcNow, 20, null, [new(recipe.Id, 20)], 0, [], "consume-edited-batch"));
        await Assert.ThrowsAsync<WorkshopConflictException>(() => service.UpdateProductionAsync("account-1",
            created.Session.Id,
            new(new(recipe.Id, DateTime.UtcNow, 19, null, "production-edit-too-small", recipe.Version,
                    bullet.Id, [new(primerPurchase.Lots.Single().Id, 19)],
                    [new(firstBulletPurchase.Lots.Single().Id, 19)]),
                decreased.Session.Revision)));
        var projectedBatch = scenario.Read<AmmoBatch>(DbCollections.WorkshopAmmoBatches).Single();
        scenario.Replace(DbCollections.WorkshopAmmoBatches, projectedBatch with { OnHandQuantity = 1 });
        Assert.True(Assert.Single(await service.GetProductionRecordsAsync("account-1")).ProjectionPending);
    }

    [Fact]
    public async Task Production_edit_preserves_the_original_recipe_snapshot_after_recipe_changes()
    {
        var (service, _, _) = CreateService();
        var primer = await service.CreateSupplyProductAsync("account-1", "primer", null, "Primer", "snapshot-primer");
        var bullet = await service.CreateSupplyProductAsync("account-1", "bullet", null, "Bullet", "snapshot-bullet");
        var powder = await service.CreateSupplyProductAsync("account-1", "powder", null, "Powder", "snapshot-powder");
        var primerPurchase = await service.RecordSupplyPurchaseAsync("account-1", primer.Id,
            DateTime.UtcNow.AddDays(-2), 100, 10m, "USD", "snapshot-primer-purchase");
        var bulletPurchase = await service.RecordSupplyPurchaseAsync("account-1", bullet.Id,
            DateTime.UtcNow.AddDays(-1), 100, 20m, "USD", "snapshot-bullet-purchase");
        var recipe = await service.CreateRecipeAsync("account-1",
            new("Original recipe", "9mm", null, null, null, null, null, null, null, primer.Id, powder.Id));
        var produced = await service.RecordProductionWithSuppliesAsync("account-1",
            new(recipe.Id, DateTime.UtcNow, 50, null, "snapshot-production", recipe.Version,
                bullet.Id, [new(primerPurchase.Lots.Single().Id, 50)],
                [new(bulletPurchase.Lots.Single().Id, 50)]));

        var changedRecipe = await service.UpdateRecipeAsync("account-1", recipe.Id, recipe.Version,
            new("Revised recipe", "9mm", null, null, null, null, null, null, null,
                primer.Id, powder.Id, recipe.Version),
            "snapshot-recipe-edit");
        var corrected = await service.UpdateProductionAsync("account-1", produced.Session.Id,
            new(new(recipe.Id, DateTime.UtcNow, 55, "corrected quantity", "snapshot-production-edit",
                    produced.Session.RecipeSnapshot.Version, bullet.Id,
                    [new(primerPurchase.Lots.Single().Id, 55)],
                    [new(bulletPurchase.Lots.Single().Id, 55)]),
                produced.Session.Revision));

        Assert.Equal(recipe.Version + 1, changedRecipe.Version);
        Assert.Equal("Original recipe", corrected.Session.RecipeSnapshot.Name);
        Assert.Equal(recipe.Version, corrected.Session.RecipeSnapshot.Version);
        Assert.Equal("Revised recipe", changedRecipe.Name);
        Assert.Single(Assert.Single(await service.GetProductionRecordsAsync("account-1")).Corrections);
    }

    [Fact]
    public async Task Inventory_costs_use_consumption_facts_not_manual_count_adjustments_and_component_corrections_are_single_spend()
    {
        var (service, scenario, _) = CreateService();
        var primer = await service.CreateSupplyProductAsync("account-1", "primer", null, "Primer", "inventory-primer");
        var bullet = await service.CreateSupplyProductAsync("account-1", "bullet", null, "Bullet", "inventory-bullet");
        var powder = await service.CreateSupplyProductAsync("account-1", "powder", null, "Powder", "inventory-powder");
        var primerPurchase = await service.RecordSupplyPurchaseAsync("account-1", primer.Id,
            DateTime.UtcNow.AddDays(-3), 10, 10m, "USD", "inventory-primer-purchase");
        var bulletPurchase = await service.RecordSupplyPurchaseAsync("account-1", bullet.Id,
            DateTime.UtcNow.AddDays(-2), 10, 20m, "USD", "inventory-bullet-purchase");
        await service.RecordSupplyPurchaseAsync("account-1", powder.Id,
            DateTime.UtcNow.AddDays(-1), 1, 5m, "USD", "inventory-powder-purchase");
        var recipe = await service.CreateRecipeAsync("account-1",
            new("Inventory test", "9mm", null, null, null, null, null, null, null, primer.Id, powder.Id));
        var componentDefinition = await service.CreateComponentDefinitionAsync("account-1",
            new("Part", null, null, null, null, WorkshopTrackingMode.Manual, null));
        var component = await service.CreateComponentAsync("account-1",
            new(componentDefinition.Id, null, null, null, null, null, null, null, 0, null));
        var componentPrice = await service.RecordComponentPurchaseAsync("account-1", component.Id,
            DateTime.UtcNow.AddDays(-5), 15m, "USD", "component-purchase-1");
        var componentPurchaseActivity = scenario.Read<WorkshopActivity>(DbCollections.WorkshopActivities)
            .Single(x => x.Type == WorkshopOperationType.ComponentPurchaseRecorded);
        Assert.Equal("Component purchase recorded", componentPurchaseActivity.Summary);
        Assert.Equal("Part", componentPurchaseActivity.EntityLabel);
        var created = await service.RecordProductionWithSuppliesAsync("account-1",
            new(recipe.Id, DateTime.UtcNow, 4, null, "inventory-production", recipe.Version,
                bullet.Id, [new(primerPurchase.Lots.Single().Id, 4)],
                [new(bulletPurchase.Lots.Single().Id, 4)]));
        var afterProduction = await service.GetInventoryAsync("account-1");
        Assert.Equal(17m, Assert.Single(afterProduction.PartialConsumptionCosts).Total);
        Assert.Equal(50m, Assert.Single(afterProduction.Spending).Total);

        var primerLot = scenario.Read<WorkshopSupplyLot>(DbCollections.WorkshopSupplyLots)
            .Single(x => x.Id == primerPurchase.Lots.Single().Id);
        await service.AdjustSupplyStockAsync("account-1", primerLot.Id, 3, primerLot.Revision,
            "physical recount", "inventory-adjust");
        var stockActivity = scenario.Read<WorkshopActivity>(DbCollections.WorkshopActivities)
            .Single(x => x.Type == WorkshopOperationType.SupplyStockAdjusted);
        Assert.Equal("Supply stock corrected", stockActivity.Summary);
        Assert.Equal("Primer", stockActivity.EntityLabel);
        var afterAdjustment = await service.GetInventoryAsync("account-1");
        Assert.Equal(17m, Assert.Single(afterAdjustment.PartialConsumptionCosts).Total);

        var corrected = await service.CorrectComponentPurchaseAsync("account-1", component.Id,
            componentPrice.PurchaseDate, 17m, "USD", componentPrice.Revision, "receipt correction",
            "component-correction");
        var afterCorrection = await service.GetInventoryAsync("account-1");
        Assert.Equal(2, Assert.Single(afterCorrection.ComponentPurchases).History.Count);
        Assert.Equal(52m, Assert.Single(afterCorrection.Spending).Total);
        Assert.Equal(17m, corrected.TotalPrice);
        Assert.Equal(4, created.Session.QuantityProduced);

        var correctedWithoutReason = await service.CorrectComponentPurchaseAsync("account-1", component.Id,
            corrected.PurchaseDate, corrected.TotalPrice, corrected.Currency, corrected.Revision, null,
            "component-correction-no-reason");
        Assert.Null(correctedWithoutReason.History.Last().Reason);
    }

    [Fact]
    public async Task Packaged_primer_and_bullet_purchases_track_stock_and_cost_per_unit()
    {
        var (service, _, _) = CreateService();
        var primer = await service.CreateSupplyProductAsync("account-1", "primer", null, "Primer", "pack-primer");
        var bullet = await service.CreateSupplyProductAsync("account-1", "bullet", "Fiocchi", "Bullet", "pack-bullet");
        var powder = await service.CreateSupplyProductAsync("account-1", "powder", null, "Powder", "pack-powder");
        var date = DateTime.UtcNow;
        var primerPurchase = await service.RecordSupplyPurchaseAsync("account-1", primer.Id, date,
            null, 4, 1500, 60m, "EUR", "pack-primer-purchase");
        var bulletPurchase = await service.RecordSupplyPurchaseAsync("account-1", bullet.Id, date,
            null, 4, 1500, 120m, "EUR", "pack-bullet-purchase");
        await service.RecordSupplyPurchaseAsync("account-1", powder.Id, date, 1, 5m, "EUR", "pack-powder-purchase");
        var recipe = await service.CreateRecipeAsync("account-1",
            new("Pack test", "9mm", null, null, null, null, null, null, null, primer.Id, powder.Id));

        await service.RecordProductionWithSuppliesAsync("account-1",
            new(recipe.Id, date, 100, null, "pack-production", recipe.Version, bullet.Id,
                [new(primerPurchase.Lots.Single().Id, 100)],
                [new(bulletPurchase.Lots.Single().Id, 100)]));

        var inventory = await service.GetInventoryAsync("account-1");
        var primerLot = inventory.Lots.Single(x => x.ProductId == primer.Id);
        var bulletLot = inventory.Lots.Single(x => x.ProductId == bullet.Id);
        Assert.Equal(6000L, primerPurchase.Purchase.Quantity);
        Assert.Equal(4, primerPurchase.Purchase.PackageCount);
        Assert.Equal(1500L, primerPurchase.Purchase.UnitsPerPackage);
        Assert.Equal(6000L, primerLot.PurchasedQuantity);
        Assert.Equal(5900L, primerLot.AvailableQuantity);
        Assert.Equal(4, bulletLot.PackageCount);
        Assert.Equal(1500L, bulletLot.UnitsPerPackage);
        Assert.Equal(5900L, bulletLot.AvailableQuantity);
        Assert.Equal(185m, Assert.Single(inventory.Spending).Total);
        Assert.Equal(8m, Assert.Single(inventory.PartialConsumptionCosts).Total);
    }

    [Fact]
    public async Task Supply_products_can_be_edited_and_deleted_until_referenced()
    {
        var (service, scenario, _) = CreateService();
        var product = await service.CreateSupplyProductAsync("account-1", "primer", null, "Primer", "lifecycle-product");

        var updated = await service.UpdateSupplyProductAsync("account-1", product.Id,
            "Fiocchi", "Small rifle primer", product.Revision, "lifecycle-edit");
        var retried = await service.UpdateSupplyProductAsync("account-1", product.Id,
            "Fiocchi", "Small rifle primer", product.Revision, "lifecycle-edit");

        var activities = scenario.Read<WorkshopActivity>(DbCollections.WorkshopActivities);
        var createdActivity = activities.Single(x => x.Type == WorkshopOperationType.SupplyProductCreated);
        var updatedActivity = activities.Single(x => x.Type == WorkshopOperationType.SupplyProductUpdated);
        Assert.Equal("Supply product added", createdActivity.Summary);
        Assert.Equal("Primer", createdActivity.EntityLabel);
        Assert.Equal("Supply product updated", updatedActivity.Summary);
        Assert.Equal("Fiocchi Small rifle primer", updatedActivity.EntityLabel);
        Assert.Equal("Fiocchi", updated.Brand);
        Assert.Equal("Small rifle primer", updated.Name);
        Assert.Equal(updated.Revision, retried.Revision);
        Assert.Equal(product.Type, updated.Type);
        await service.DeleteSupplyProductAsync("account-1", product.Id, updated.Revision, "lifecycle-delete");
        await service.DeleteSupplyProductAsync("account-1", product.Id, updated.Revision, "lifecycle-delete");
        var deletedActivity = scenario.Read<WorkshopActivity>(DbCollections.WorkshopActivities)
            .Single(x => x.Type == WorkshopOperationType.SupplyProductDeleted);
        Assert.Equal("Supply product deleted", deletedActivity.Summary);
        Assert.Equal("Fiocchi Small rifle primer", deletedActivity.EntityLabel);
        Assert.Empty(scenario.Read<WorkshopSupplyProduct>(DbCollections.WorkshopSupplyProducts));
    }

    [Fact]
    public async Task Referenced_supply_products_can_be_archived_but_not_deleted()
    {
        var (service, scenario, _) = CreateService();
        var product = await service.CreateSupplyProductAsync("account-1", "primer", null, "Primer", "archive-product");
        await service.RecordSupplyPurchaseAsync("account-1", product.Id,
            DateTime.UtcNow, 10, 10m, "EUR", "archive-product-purchase");

        var archived = await service.SetSupplyProductArchivedAsync(
            "account-1", product.Id, true, product.Revision, "archive-product-command");

        Assert.True(archived.Archived);
        await Assert.ThrowsAsync<WorkshopConflictException>(() =>
            service.DeleteSupplyProductAsync("account-1", product.Id, archived.Revision, "delete-referenced-product"));
        Assert.Single(scenario.Read<WorkshopSupplyProduct>(DbCollections.WorkshopSupplyProducts));
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
