using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Models.Models.Serializers;
using System.Text.Json.Serialization;

namespace MorWalPizVideo.Models.Models.Workshop;

public enum WorkshopTrackingMode { Counter, Interval, Manual }
public enum WorkshopComponentStatus { Inventory, Installed, Maintenance, Retired }
public enum WorkshopRecipeStatus { Active, Archived }
public enum WorkshopMaintenanceEventType { Inspection, Cleaning, ComponentMaintenance, Replacement, CounterAdjustment, General, Malfunction }
public enum WorkshopOperationType { FirearmCreated, FirearmUpdated, FirearmArchived, ComponentDefinitionCreated, ComponentDefinitionUpdated, ComponentDefinitionArchived, ComponentDefinitionDeleted, ComponentCreated, ComponentUpdated, ComponentArchived, ComponentDeleted, ComponentInstalled, ComponentRemoved, MaintenanceRecorded, CounterAdjusted, RecipeCreated, RecipeUpdated, RecipeArchived, ProductionRecorded, AllocationRecorded, AllocationReleased, AllocationReallocated, ShootingRecorded, ShootingCorrected, StockAdjusted, ComponentSwapped, SupplyProductCreated, SupplyPurchaseRecorded, SupplyStockAdjusted, SupplyLotStatusChanged, ComponentPurchaseRecorded, ComponentPurchaseCorrected, ProductionEdited }
public enum WorkshopAggregateKind { Firearm, Component, Batch, Allocation, SupplyLot }
public enum WorkshopProjectionState { Current, Pending, Inconsistent }
public enum WorkshopSupplyType { Primer, Bullet, Powder, Case, CommercialAmmunition }
public enum WorkshopSupplyLotStatus { Available, Empty }

public sealed record Firearm : BaseEntity
{
    [BsonElement("accountId")] public string AccountId { get; init; } = string.Empty;
    [BsonElement("manufacturer")] public string Manufacturer { get; init; } = string.Empty;
    [BsonElement("model")] public string Model { get; init; } = string.Empty;
    [BsonElement("caliber")] public string Caliber { get; init; } = string.Empty;
    [BsonElement("category")] public string? Category { get; init; }
    [BsonElement("nickname")] public string? Nickname { get; init; }
    [BsonElement("purchaseDate")] public DateTime? PurchaseDate { get; init; }
    [BsonElement("notes")] public string? Notes { get; init; }
    [BsonElement("totalRounds")] public long TotalRounds { get; init; }
    [BsonElement("revision")] public long Revision { get; init; }
    [BsonElement("archived")] public bool Archived { get; init; }
}

public sealed record ComponentDefinition : BaseEntity
{
    [BsonElement("accountId")] public string AccountId { get; init; } = string.Empty;
    [BsonElement("name")] public string Name { get; init; } = string.Empty;
    [BsonElement("manufacturer")] public string? Manufacturer { get; init; }
    [BsonElement("model")] public string? Model { get; init; }
    [BsonElement("description")] public string? Description { get; init; }
    [BsonElement("defaultServiceInterval")] public long? DefaultServiceInterval { get; init; }
    [BsonElement("trackingMode")] public WorkshopTrackingMode TrackingMode { get; init; }
    [BsonElement("notes")] public string? Notes { get; init; }
    [BsonElement("compatibilityTags")] public IReadOnlyList<string> CompatibilityTags { get; init; } = [];
    [BsonElement("archived")] public bool Archived { get; init; }
    [BsonElement("revision")] public long Revision { get; init; }
}

public sealed record ComponentInstance : BaseEntity
{
    [BsonElement("accountId")] public string AccountId { get; init; } = string.Empty;
    [BsonElement("definitionId")] public string DefinitionId { get; init; } = string.Empty;
    [BsonElement("identificationNote")] public string? IdentificationNote { get; init; }
    [BsonElement("manufacturer")] public string? Manufacturer { get; init; }
    [BsonElement("model")] public string? Model { get; init; }
    [BsonElement("compatibilityTags")] public IReadOnlyList<string> CompatibilityTags { get; init; } = [];
    [BsonElement("totalRounds")] public long TotalRounds { get; init; }
    [BsonElement("serviceInterval")] public long? ServiceInterval { get; init; }
    [BsonElement("trackingMode")] public WorkshopTrackingMode TrackingMode { get; init; }
    [BsonElement("status")] public WorkshopComponentStatus Status { get; init; }
    [BsonElement("archived")] public bool Archived { get; init; }
    [BsonElement("notes")] public string? Notes { get; init; }
    [BsonElement("revision")] public long Revision { get; init; }
}

public sealed record WorkshopSupplyProduct : BaseEntity
{
    [BsonElement("accountId")] public string AccountId { get; init; } = string.Empty;
    [BsonElement("type")] public WorkshopSupplyType Type { get; init; }
    [BsonElement("brand")] public string? Brand { get; init; }
    [BsonElement("name")] public string Name { get; init; } = string.Empty;
    [BsonElement("archived")] public bool Archived { get; init; }
    [BsonElement("revision")] public long Revision { get; init; }
}

public sealed record WorkshopSupplyPurchase : BaseEntity
{
    [BsonElement("accountId")] public string AccountId { get; init; } = string.Empty;
    [BsonElement("productId")] public string ProductId { get; init; } = string.Empty;
    [BsonElement("purchasedAt")] public DateTime PurchasedAt { get; init; }
    [BsonElement("quantity")] public long Quantity { get; init; }
    [BsonElement("totalPrice")] public decimal TotalPrice { get; init; }
    [BsonElement("currency")] public string Currency { get; init; } = string.Empty;
    [BsonElement("lotIds")] public IReadOnlyList<string> LotIds { get; init; } = [];
}

public sealed record WorkshopSupplyLot : BaseEntity
{
    [BsonElement("accountId")] public string AccountId { get; init; } = string.Empty;
    [BsonElement("purchaseId")] public string PurchaseId { get; init; } = string.Empty;
    [BsonElement("productId")] public string ProductId { get; init; } = string.Empty;
    [BsonElement("purchasedAt")] public DateTime PurchasedAt { get; init; }
    [BsonElement("purchasedQuantity")] public long PurchasedQuantity { get; init; }
    [BsonElement("availableQuantity")] public long AvailableQuantity { get; init; }
    [BsonElement("totalPrice")] public decimal TotalPrice { get; init; }
    [BsonElement("currency")] public string Currency { get; init; } = string.Empty;
    [BsonElement("status")] public WorkshopSupplyLotStatus Status { get; init; } = WorkshopSupplyLotStatus.Available;
    [BsonElement("itemCount")] public long? ItemCount { get; init; }
    [BsonElement("revision")] public long Revision { get; init; }
}

public sealed record WorkshopComponentPurchase : BaseEntity
{
    [BsonElement("accountId")] public string AccountId { get; init; } = string.Empty;
    [BsonElement("componentId")] public string ComponentId { get; init; } = string.Empty;
    [BsonElement("purchaseDate")] public DateTime PurchaseDate { get; init; }
    [BsonElement("totalPrice")] public decimal TotalPrice { get; init; }
    [BsonElement("currency")] public string Currency { get; init; } = string.Empty;
    [BsonElement("revision")] public long Revision { get; init; }
    [BsonElement("history")] public IReadOnlyList<WorkshopComponentPurchaseRevision> History { get; init; } = [];
}

public sealed record WorkshopComponentPurchaseRevision(
    long Revision, DateTime PurchaseDate, decimal TotalPrice, string Currency, string? Reason, DateTime RecordedAt);

public sealed record WorkshopInventoryFact(
    string Type,
    string? ProductId = null,
    string? PurchaseId = null,
    string? LotId = null,
    string? ComponentId = null,
    decimal? PreviousAmount = null,
    decimal? CurrentAmount = null,
    string? Currency = null,
    string? Reason = null);

public sealed record ComponentInstallation : BaseEntity
{
    [BsonElement("accountId")] public string AccountId { get; init; } = string.Empty;
    [BsonElement("componentId")] public string ComponentId { get; init; } = string.Empty;
    [BsonElement("firearmId")] public string FirearmId { get; init; } = string.Empty;
    [BsonElement("installedAt")] public DateTime InstalledAt { get; init; }
    [BsonElement("removedAt")] public DateTime? RemovedAt { get; init; }
    [BsonElement("installationNotes")] public string? InstallationNotes { get; init; }
    [BsonElement("removalNotes")] public string? RemovalNotes { get; init; }
    [BsonElement("revision")] public long Revision { get; init; }
}

public sealed record MaintenanceEvent : BaseEntity
{
    [BsonElement("accountId")] public string AccountId { get; init; } = string.Empty;
    [BsonElement("firearmId")] public string? FirearmId { get; init; }
    [BsonElement("componentId")] public string? ComponentId { get; init; }
    [BsonElement("eventType")] public WorkshopMaintenanceEventType EventType { get; init; }
    [BsonElement("date")] public DateTime Date { get; init; }
    [BsonElement("roundCountAtEvent")] public long? RoundCountAtEvent { get; init; }
    [BsonElement("description")] public string Description { get; init; } = string.Empty;
    [BsonElement("notes")] public string? Notes { get; init; }
}

public sealed record RecipeSnapshot(
    string Name, string Caliber, [property: BsonSerializer(typeof(TolerantDecimalStringSerializer))] string? Oal, decimal? BulletWeight, string? BulletForm,
    string? Primer, string? Powder, decimal? PowderWeight, string? Notes,
    string? PrimerProductId = null, string? PowderProductId = null,
    long Version = 1)
{
    [BsonExtraElements, JsonIgnore]
    public BsonDocument? LegacyFields { get; init; }
}

public sealed record AmmoRecipe : BaseEntity
{
    [BsonElement("accountId")] public string AccountId { get; init; } = string.Empty;
    [BsonElement("name")] public string Name { get; init; } = string.Empty;
    [BsonElement("caliber")] public string Caliber { get; init; } = string.Empty;
    [BsonElement("oal"), BsonSerializer(typeof(TolerantDecimalStringSerializer))] public string? Oal { get; init; }
    [BsonElement("bulletWeight")] public decimal? BulletWeight { get; init; }
    [BsonElement("bulletForm")] public string? BulletForm { get; init; }
    [BsonElement("primer")] public string? Primer { get; init; }
    [BsonElement("powder")] public string? Powder { get; init; }
    [BsonElement("primerProductId")] public string? PrimerProductId { get; init; }
    [BsonElement("powderProductId")] public string? PowderProductId { get; init; }
    [BsonElement("powderWeight")] public decimal? PowderWeight { get; init; }
    [BsonElement("notes")] public string? Notes { get; init; }
    [BsonElement("status")] public WorkshopRecipeStatus Status { get; init; } = WorkshopRecipeStatus.Active;
    [BsonElement("version")] public long Version { get; init; } = 1;
    [BsonElement("updatedAt")] public DateTime UpdatedAt { get; init; }
    [BsonExtraElements, JsonIgnore] public BsonDocument? LegacyFields { get; init; }
}

public sealed record AmmoRecipeVersion : BaseEntity
{
    [BsonElement("accountId")] public string AccountId { get; init; } = string.Empty;
    [BsonElement("recipeId")] public string RecipeId { get; init; } = string.Empty;
    [BsonElement("version")] public long Version { get; init; }
    [BsonElement("snapshot")] public RecipeSnapshot Snapshot { get; init; } = new("", "", null, null, null, null, null, null, null, null);
}

public sealed record WorkshopRecipeJournalEntry : BaseEntity
{
    [BsonElement("accountId")] public string AccountId { get; init; } = string.Empty;
    [BsonElement("recipeId")] public string RecipeId { get; init; } = string.Empty;
    [BsonElement("entryDate")] public string EntryDate { get; init; } = string.Empty;
    [BsonElement("timeOfDay")] public string? TimeOfDay { get; init; }
    [BsonElement("weather")] public string? Weather { get; init; }
    [BsonElement("temperature")] public string? Temperature { get; init; }
    [BsonElement("temperatureUnit")] public string? TemperatureUnit { get; init; }
    [BsonElement("powerFactor")] public string? PowerFactor { get; init; }
    [BsonElement("notes")] public string? Notes { get; init; }
    [BsonElement("clientIdempotencyKey")] public string ClientIdempotencyKey { get; init; } = string.Empty;
    [BsonElement("payloadHash")] public string PayloadHash { get; init; } = string.Empty;
}

public sealed record ProductionSession : BaseEntity
{
    [BsonElement("accountId")] public string AccountId { get; init; } = string.Empty;
    [BsonElement("recipeId")] public string RecipeId { get; init; } = string.Empty;
    [BsonElement("recipeSnapshot")] public RecipeSnapshot RecipeSnapshot { get; init; } = new("", "", null, null, null, null, null, null, null, null);
    [BsonElement("productionDate")] public DateTime ProductionDate { get; init; }
    [BsonElement("quantityProduced")] public long QuantityProduced { get; init; }
    [BsonElement("notes")] public string? Notes { get; init; }
    [BsonElement("batchId")] public string BatchId { get; init; } = string.Empty;
    [BsonElement("bulletProductId")] public string BulletProductId { get; init; } = string.Empty;
    [BsonElement("primerLotAllocations")] public IReadOnlyList<WorkshopLotAllocation> PrimerLotAllocations { get; init; } = [];
    [BsonElement("bulletLotAllocations")] public IReadOnlyList<WorkshopLotAllocation> BulletLotAllocations { get; init; } = [];
    [BsonElement("revision")] public long Revision { get; init; }
}

public sealed record WorkshopLotAllocation(string LotId, long Quantity, long LotRevision);

public sealed record WorkshopProductionCorrectionFact(
    long ExpectedRevision,
    DateTime PreviousProductionDate,
    long PreviousQuantity,
    string PreviousBulletProductId,
    string? PreviousNotes,
    IReadOnlyList<WorkshopLotAllocation> PreviousPrimerLotAllocations,
    IReadOnlyList<WorkshopLotAllocation> PreviousBulletLotAllocations,
    DateTime ProductionDate,
    long Quantity,
    string BulletProductId,
    string? Notes,
    IReadOnlyList<WorkshopLotAllocation> PrimerLotAllocations,
    IReadOnlyList<WorkshopLotAllocation> BulletLotAllocations);

public sealed record AmmoBatch : BaseEntity
{
    [BsonElement("accountId")] public string AccountId { get; init; } = string.Empty;
    [BsonElement("recipeId")] public string RecipeId { get; init; } = string.Empty;
    [BsonElement("recipeSnapshot")] public RecipeSnapshot RecipeSnapshot { get; init; } = new("", "", null, null, null, null, null, null, null, null);
    [BsonElement("productionSessionId")] public string ProductionSessionId { get; init; } = string.Empty;
    [BsonElement("productionDate")] public DateTime ProductionDate { get; init; }
    [BsonElement("producedQuantity")] public long ProducedQuantity { get; init; }
    [BsonElement("onHandQuantity")] public long OnHandQuantity { get; init; }
    [BsonElement("revision")] public long Revision { get; init; }
    [BsonElement("notes")] public string? Notes { get; init; }
    [BsonElement("bulletProductId")] public string BulletProductId { get; init; } = string.Empty;
    [BsonElement("primerLotAllocations")] public IReadOnlyList<WorkshopLotAllocation> PrimerLotAllocations { get; init; } = [];
    [BsonElement("bulletLotAllocations")] public IReadOnlyList<WorkshopLotAllocation> BulletLotAllocations { get; init; } = [];
}

public sealed record AmmoAllocation : BaseEntity
{
    [BsonElement("accountId")] public string AccountId { get; init; } = string.Empty;
    [BsonElement("batchId")] public string BatchId { get; init; } = string.Empty;
    [BsonElement("firearmId")] public string FirearmId { get; init; } = string.Empty;
    [BsonElement("quantity")] public long Quantity { get; init; }
    [BsonElement("consumedQuantity")] public long ConsumedQuantity { get; init; }
    [BsonElement("releasedQuantity")] public long ReleasedQuantity { get; init; }
    [BsonElement("allocatedAt")] public DateTime AllocatedAt { get; init; }
    [BsonElement("notes")] public string? Notes { get; init; }
    [BsonElement("revision")] public long Revision { get; init; }
    public long ActiveQuantity => Quantity - ConsumedQuantity - ReleasedQuantity;
}

public sealed record ShootingConsumption(string? BatchId, long Quantity, string? AllocationId, string? RecipeId = null);
public sealed record ShootingUsageSegment(string ComponentId, string InstallationId, long Quantity);

public sealed record ShootingSession : BaseEntity
{
    [BsonElement("accountId")] public string AccountId { get; init; } = string.Empty;
    [BsonElement("firearmId")] public string FirearmId { get; init; } = string.Empty;
    [BsonElement("date")] public DateTime Date { get; init; }
    [BsonElement("totalRounds")] public long TotalRounds { get; init; }
    [BsonElement("notes")] public string? Notes { get; init; }
    [BsonElement("consumptionRecords")] public IReadOnlyList<ShootingConsumption> ConsumptionRecords { get; init; } = [];
    [BsonElement("usageSegments")] public IReadOnlyList<ShootingUsageSegment> UsageSegments { get; init; } = [];
    [BsonElement("untrackedAmmunition")] public long UntrackedAmmunition { get; init; }
    [BsonElement("correctionOfId")] public string? CorrectionOfId { get; init; }
    [BsonElement("operationId")] public string? OperationId { get; init; }
}

public sealed record WorkshopDelta(
    WorkshopAggregateKind AggregateKind, string AggregateId, long RoundsDelta = 0,
    long StockDelta = 0, long AllocationDelta = 0, long AllocationConsumedDelta = 0,
    long AllocationReleasedDelta = 0, string? BatchId = null, string? AllocationId = null,
    string? ComponentId = null, string? InstallationId = null);

public sealed record WorkshopOperation : BaseEntity
{
    [BsonElement("accountId")] public string AccountId { get; init; } = string.Empty;
    [BsonElement("clientIdempotencyKey")] public string ClientIdempotencyKey { get; init; } = string.Empty;
    [BsonElement("payloadHash")] public string PayloadHash { get; init; } = string.Empty;
    [BsonElement("clientRequestHash")] public string? ClientRequestHash { get; init; }
    [BsonElement("operationType")] public WorkshopOperationType OperationType { get; init; }
    [BsonElement("schemaVersion")] public int SchemaVersion { get; init; } = 1;
    [BsonElement("effectiveAt")] public DateTime EffectiveAt { get; init; }
    [BsonElement("recordedAt")] public DateTime RecordedAt { get; init; }
    [BsonElement("correlationId")] public string? CorrelationId { get; init; }
    [BsonElement("originalOperationId")] public string? OriginalOperationId { get; init; }
    [BsonElement("replacementOperationId")] public string? ReplacementOperationId { get; init; }
    [BsonElement("deltas")] public IReadOnlyList<WorkshopDelta> Deltas { get; init; } = [];
    [BsonElement("inventoryFacts")] public IReadOnlyList<WorkshopInventoryFact> InventoryFacts { get; init; } = [];
    [BsonElement("productionSnapshot")] public RecipeSnapshot? ProductionSnapshot { get; init; }
    [BsonElement("productionCorrection")] public WorkshopProductionCorrectionFact? ProductionCorrection { get; init; }
    [BsonElement("sourceEntityIds")] public IReadOnlyList<string> SourceEntityIds { get; init; } = [];
    // Deterministic evidence for replay; projection status belongs to the reconciliation document.
    [BsonElement("contributionDigest")] public string ContributionDigest { get; init; } = string.Empty;
}

public sealed record WorkshopActivity : BaseEntity
{
    [BsonElement("accountId")] public string AccountId { get; init; } = string.Empty;
    [BsonElement("operationId")] public string OperationId { get; init; } = string.Empty;
    [BsonElement("type")] public WorkshopOperationType Type { get; init; }
    [BsonElement("effectiveAt")] public DateTime EffectiveAt { get; init; }
    [BsonElement("recordedAt")] public DateTime? RecordedAt { get; init; }
    [BsonElement("sourceEntityIds")] public IReadOnlyList<string> SourceEntityIds { get; init; } = [];
    [BsonElement("summary")] public string? Summary { get; init; }
    [BsonElement("entityLabel")] public string? EntityLabel { get; init; }
}

public sealed record WorkshopReconciliationState : BaseEntity
{
    [BsonElement("accountId")] public string AccountId { get; init; } = string.Empty;
    [BsonElement("state")] public WorkshopProjectionState State { get; init; }
    [BsonElement("mismatchCount")] public int MismatchCount { get; init; }
    [BsonElement("lastRunAt")] public DateTime LastRunAt { get; init; }
    [BsonElement("mismatches")] public IReadOnlyList<string> Mismatches { get; init; } = [];
}

