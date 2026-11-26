using System.ComponentModel.DataAnnotations;
using MorWalPizVideo.Models.Models.Workshop;

namespace MorWalPiz.Contracts.Contracts.Workshop;

public class CreateWorkshopFirearmRequest
{
    [Required, StringLength(120)] public string Manufacturer { get; init; } = string.Empty;
    [Required, StringLength(120)] public string Model { get; init; } = string.Empty;
    [Required, StringLength(80)] public string Caliber { get; init; } = string.Empty;
    [StringLength(120)] public string? Category { get; init; }
    [StringLength(120)] public string? Nickname { get; init; }
    public DateTime? PurchaseDate { get; init; }
    [Range(0, long.MaxValue)] public long InitialRounds { get; init; }
    [StringLength(4000)] public string? Notes { get; init; }
    [StringLength(80)] public string? IdempotencyKey { get; init; }
}

public sealed class UpdateWorkshopFirearmRequest : CreateWorkshopFirearmRequest
{
    [Range(0, long.MaxValue)] public long Revision { get; init; }
}

public sealed class ArchiveWorkshopFirearmRequest
{
    [Range(0, long.MaxValue)] public long Revision { get; init; }
    [Required, StringLength(80)] public string IdempotencyKey { get; init; } = string.Empty;
}

public sealed class CreateWorkshopComponentDefinitionRequest
{
    [Required, StringLength(160)] public string Name { get; init; } = string.Empty;
    [StringLength(120)] public string? Manufacturer { get; init; }
    [StringLength(120)] public string? Model { get; init; }
    [StringLength(2000)] public string? Description { get; init; }
    [Range(1, long.MaxValue)] public long? DefaultServiceInterval { get; init; }
    public WorkshopTrackingMode TrackingMode { get; init; } = WorkshopTrackingMode.Counter;
    [StringLength(4000)] public string? Notes { get; init; }
    public IReadOnlyList<string> CompatibilityTags { get; init; } = [];
    [StringLength(80)] public string? IdempotencyKey { get; init; }
}

public sealed class UpdateWorkshopComponentDefinitionRequest
{
    [Required, StringLength(160)] public string Name { get; init; } = string.Empty;
    [StringLength(2000)] public string? Description { get; init; }
    [StringLength(4000)] public string? Notes { get; init; }
    [Range(0, long.MaxValue)] public long Revision { get; init; }
    [Required, StringLength(80)] public string IdempotencyKey { get; init; } = string.Empty;
}

public sealed class ArchiveWorkshopComponentDefinitionRequest
{
    [Range(0, long.MaxValue)] public long Revision { get; init; }
    [Required, StringLength(80)] public string IdempotencyKey { get; init; } = string.Empty;
}

public sealed class DeleteWorkshopComponentDefinitionRequest
{
    [Range(0, long.MaxValue)] public long Revision { get; init; }
    [Required, StringLength(80)] public string IdempotencyKey { get; init; } = string.Empty;
}

public abstract class WorkshopComponentRequestBase
{
    [Required] public string DefinitionId { get; init; } = string.Empty;
    [StringLength(500)] public string? IdentificationNote { get; init; }
    [StringLength(120)] public string? Manufacturer { get; init; }
    [StringLength(120)] public string? Model { get; init; }
    [Range(1, long.MaxValue)] public long? ServiceInterval { get; init; }
    public WorkshopTrackingMode? TrackingMode { get; init; }
    [Range(0, long.MaxValue)] public long InitialRounds { get; init; }
    [StringLength(4000)] public string? Notes { get; init; }
    public IReadOnlyList<string> CompatibilityTags { get; init; } = [];
}

public sealed class CreateWorkshopComponentRequest : WorkshopComponentRequestBase
{
    [StringLength(80)] public string? IdempotencyKey { get; init; }
}

public sealed class CreateAndInstallWorkshopComponentRequest : WorkshopComponentRequestBase
{
    public string? FirearmId { get; init; }
    public DateTime InstalledAt { get; init; }
    [StringLength(1000)] public string? InstallationNotes { get; init; }
    [Required, StringLength(80)] public string IdempotencyKey { get; init; } = string.Empty;
}

public sealed class UpdateWorkshopComponentRequest : WorkshopComponentRequestBase
{
    [Range(0, long.MaxValue)] public long? TotalRounds { get; init; }
    [Range(0, long.MaxValue)] public long Revision { get; init; }
    [Required, StringLength(80)] public string IdempotencyKey { get; init; } = string.Empty;
}

public sealed class ArchiveWorkshopComponentRequest
{
    [Range(0, long.MaxValue)] public long Revision { get; init; }
    [Required, StringLength(80)] public string IdempotencyKey { get; init; } = string.Empty;
}

public sealed class DeleteWorkshopComponentRequest
{
    [Range(0, long.MaxValue)] public long Revision { get; init; }
    [Required, StringLength(80)] public string IdempotencyKey { get; init; } = string.Empty;
}

public sealed record CreateAndInstallWorkshopComponentResponse(
    ComponentInstance Component,
    ComponentInstallation? Installation);

public sealed class InstallWorkshopComponentRequest
{
    [Required] public string ComponentId { get; init; } = string.Empty;
    [Required] public string FirearmId { get; init; } = string.Empty;
    public DateTime InstalledAt { get; init; }
    [StringLength(1000)] public string? InstallationNotes { get; init; }
    [Required, StringLength(80)] public string IdempotencyKey { get; init; } = string.Empty;
}

public sealed class RemoveWorkshopComponentRequest
{
    public DateTime RemovedAt { get; init; }
    [StringLength(1000)] public string? RemovalNotes { get; init; }
    [Required, StringLength(80)] public string IdempotencyKey { get; init; } = string.Empty;
    [Range(0, long.MaxValue)] public long Revision { get; init; }
}

public sealed class RecordWorkshopMaintenanceRequest
{
    public string? FirearmId { get; init; }
    public string? ComponentId { get; init; }
    public WorkshopMaintenanceEventType EventType { get; init; }
    public DateTime Date { get; init; }
    [Range(0, long.MaxValue)] public long? RoundCountAtEvent { get; init; }
    [Required, StringLength(2000)] public string Description { get; init; } = string.Empty;
    [StringLength(4000)] public string? Notes { get; init; }
    [Required, StringLength(80)] public string IdempotencyKey { get; init; } = string.Empty;
}

public sealed class CreateWorkshopMaintenanceRequest
{
    public string? FirearmId { get; init; }
    public string? ComponentId { get; init; }
    public WorkshopMaintenanceEventType EventType { get; init; }
    public DateTime Date { get; init; }
    [Range(0, long.MaxValue)] public long? RoundCountAtEvent { get; init; }
    [Required, StringLength(2000)] public string Description { get; init; } = string.Empty;
    [StringLength(4000)] public string? Notes { get; init; }
    [Required, StringLength(80)] public string IdempotencyKey { get; init; } = string.Empty;
}

public class WorkshopRecipeRequest
{
    [Required, StringLength(160)] public string Name { get; init; } = string.Empty;
    [Required, StringLength(80)] public string Caliber { get; init; } = string.Empty;
    [StringLength(80)] public string? Oal { get; init; }
    public decimal? BulletWeight { get; init; }
    [StringLength(160)] public string? BulletForm { get; init; }
    [StringLength(160)] public string? Primer { get; init; }
    [StringLength(160)] public string? Powder { get; init; }
    [StringLength(80)] public string? PrimerProductId { get; init; }
    [StringLength(80)] public string? PowderProductId { get; init; }
    public decimal? PowderWeight { get; init; }
    [StringLength(4000)] public string? Notes { get; init; }
}

public sealed class UpdateWorkshopRecipeRequest : WorkshopRecipeRequest
{
    [Range(1, long.MaxValue)] public long Version { get; init; }
    [Required, StringLength(80)] public string IdempotencyKey { get; init; } = string.Empty;
}

public sealed class ArchiveWorkshopRecipeRequest
{
    [Range(1, long.MaxValue)] public long Version { get; init; }
    [Required, StringLength(80)] public string IdempotencyKey { get; init; } = string.Empty;
}

public class RecordWorkshopProductionRequest
{
    [Required] public string RecipeId { get; init; } = string.Empty;
    [Range(1, long.MaxValue)] public long RecipeVersion { get; init; }
    public DateTime ProductionDate { get; init; }
    [Range(1, long.MaxValue)] public long QuantityProduced { get; init; }
    [Required, StringLength(80)] public string BulletProductId { get; init; } = string.Empty;
    public IReadOnlyList<WorkshopLotAllocationRequest> PrimerLotAllocations { get; init; } = [];
    public IReadOnlyList<WorkshopLotAllocationRequest> BulletLotAllocations { get; init; } = [];
    [StringLength(4000)] public string? Notes { get; init; }
    [Required, StringLength(80)] public string IdempotencyKey { get; init; } = string.Empty;
}

public sealed class UpdateWorkshopProductionRequest : RecordWorkshopProductionRequest
{
    [Range(0, long.MaxValue)] public long ExpectedRevision { get; init; }
}

public sealed class WorkshopLotAllocationRequest
{
    [Required, StringLength(80)] public string LotId { get; init; } = string.Empty;
    [Range(1, long.MaxValue)] public long Quantity { get; init; }
}

public sealed class CreateWorkshopSupplyProductRequest
{
    [Required, StringLength(40)] public string Type { get; init; } = string.Empty;
    [StringLength(120)] public string? Brand { get; init; }
    [Required, StringLength(160)] public string Name { get; init; } = string.Empty;
    [Required, StringLength(80)] public string IdempotencyKey { get; init; } = string.Empty;
}

public sealed class RecordWorkshopSupplyPurchaseRequest
{
    [Required, StringLength(80)] public string ProductId { get; init; } = string.Empty;
    public DateTime PurchaseDate { get; init; }
    [Range(1, 10000)] public long Quantity { get; init; }
    [Range(typeof(decimal), "0", "999999999999.99")] public decimal TotalPrice { get; init; }
    [Required, RegularExpression("^[A-Z]{3}$")] public string Currency { get; init; } = string.Empty;
    [Required, StringLength(80)] public string IdempotencyKey { get; init; } = string.Empty;
}

public sealed class AdjustWorkshopSupplyStockRequest
{
    [Required, StringLength(80)] public string LotId { get; init; } = string.Empty;
    [Range(0, long.MaxValue)] public long PhysicalCount { get; init; }
    [Range(0, long.MaxValue)] public long ExpectedRevision { get; init; }
    [Required, StringLength(500)] public string Reason { get; init; } = string.Empty;
    [Required, StringLength(80)] public string IdempotencyKey { get; init; } = string.Empty;
}

public sealed class SetWorkshopSupplyLotStatusRequest
{
    public bool IsEmpty { get; init; }
    [Range(0, long.MaxValue)] public long ExpectedRevision { get; init; }
    [Required, StringLength(80)] public string IdempotencyKey { get; init; } = string.Empty;
}

public sealed class RecordWorkshopComponentPurchaseRequest
{
    public DateTime PurchaseDate { get; init; }
    [Range(typeof(decimal), "0", "999999999999.99")] public decimal TotalPrice { get; init; }
    [Required, RegularExpression("^[A-Z]{3}$")] public string Currency { get; init; } = string.Empty;
    [Required, StringLength(80)] public string IdempotencyKey { get; init; } = string.Empty;
}

public sealed class CorrectWorkshopComponentPurchaseRequest
{
    public DateTime PurchaseDate { get; init; }
    [Range(typeof(decimal), "0", "999999999999.99")] public decimal TotalPrice { get; init; }
    [Required, RegularExpression("^[A-Z]{3}$")] public string Currency { get; init; } = string.Empty;
    [Range(1, long.MaxValue)] public long ExpectedRevision { get; init; }
    [Required, StringLength(500)] public string Reason { get; init; } = string.Empty;
    [Required, StringLength(80)] public string IdempotencyKey { get; init; } = string.Empty;
}

public sealed record WorkshopInventoryProductContract(
    string Id, string Type, string? Brand, string Name, bool Archived);

public sealed record WorkshopInventoryLotContract(
    string Id, string ProductId, DateTime PurchaseDate, long PurchasedQuantity,
    long AvailableQuantity, decimal TotalPrice, string Currency, bool IsEmpty, long? ItemCount, long Revision);

public sealed record WorkshopComponentPurchaseContract(
    string Id, string ComponentId, DateTime PurchaseDate, decimal TotalPrice, string Currency, long Revision,
    IReadOnlyList<WorkshopComponentPurchaseRevision> History);

public sealed record WorkshopCurrencyTotalContract(string Currency, decimal Total);

public sealed record WorkshopInventoryContract(
    IReadOnlyList<WorkshopInventoryProductContract> Products,
    IReadOnlyList<WorkshopInventoryLotContract> Lots,
    IReadOnlyList<WorkshopComponentPurchaseContract> ComponentPurchases,
    IReadOnlyList<WorkshopCurrencyTotalContract> Spending,
    IReadOnlyList<WorkshopCurrencyTotalContract> PartialConsumptionCosts,
    IReadOnlyList<string> MissingComponentPrices);

public sealed record WorkshopOperationMutationContract(
    string Id, WorkshopOperationType OperationType, DateTime EffectiveAt, DateTime RecordedAt,
    string ContributionDigest, bool Idempotent, ProductionSession Session, AmmoBatch Batch);

public sealed record WorkshopProductionCorrectionContract(
    string OperationId, DateTime RecordedAt, WorkshopProductionCorrectionFact Correction);

public sealed record WorkshopProductionRecordContract(
    ProductionSession Session, IReadOnlyList<WorkshopProductionCorrectionContract> Corrections,
    bool ProjectionPending);

public sealed class AllocateWorkshopAmmoRequest
{
    [Required] public string BatchId { get; init; } = string.Empty;
    [Required] public string FirearmId { get; init; } = string.Empty;
    [Range(1, long.MaxValue)] public long Quantity { get; init; }
    [StringLength(2000)] public string? Notes { get; init; }
    [Required, StringLength(80)] public string IdempotencyKey { get; init; } = string.Empty;
}

public sealed class ReleaseWorkshopAllocationRequest
{
    [Range(1, long.MaxValue)] public long Quantity { get; init; }
    [Range(0, long.MaxValue)] public long Revision { get; init; }
    [Required, StringLength(80)] public string IdempotencyKey { get; init; } = string.Empty;
}

public sealed class ReallocateWorkshopAmmoRequest
{
    [Required] public string SourceAllocationId { get; init; } = string.Empty;
    [Required] public string DestinationFirearmId { get; init; } = string.Empty;
    [Range(1, long.MaxValue)] public long Quantity { get; init; }
    [Required, StringLength(80)] public string IdempotencyKey { get; init; } = string.Empty;
}

public sealed class WorkshopConsumptionRequest
{
    [Required] public string RecipeId { get; init; } = string.Empty;
    [Range(1, long.MaxValue)] public long Quantity { get; init; }
}

public sealed class WorkshopUsageSegmentRequest
{
    [Required] public string ComponentId { get; init; } = string.Empty;
    [Required] public string InstallationId { get; init; } = string.Empty;
    [Range(1, long.MaxValue)] public long Quantity { get; init; }
}

public sealed class RecordWorkshopShootingRequest
{
    [Required] public string FirearmId { get; init; } = string.Empty;
    public DateTime Date { get; init; }
    [Range(1, long.MaxValue)] public long TotalRounds { get; init; }
    [StringLength(4000)] public string? Notes { get; init; }
    public IReadOnlyList<WorkshopConsumptionRequest> AmmunitionRecords { get; init; } = [];
    // Unknown ammunition is explicit; it is never inferred from a missing batch.
    [Range(0, long.MaxValue)] public long UntrackedAmmunition { get; init; }
    public IReadOnlyList<WorkshopUsageSegmentRequest> UsageSegments { get; init; } = [];
    [Required, StringLength(80)] public string IdempotencyKey { get; init; } = string.Empty;
}

public sealed class CorrectWorkshopOperationRequest
{
    [Required] public string OriginalOperationId { get; init; } = string.Empty;
    public RecordWorkshopShootingRequest? ReplacementShooting { get; init; }
    [Required, StringLength(80)] public string IdempotencyKey { get; init; } = string.Empty;
}

public sealed class AdjustWorkshopCounterRequest
{
    [Required] public WorkshopAggregateKind AggregateKind { get; init; }
    [Required] public string AggregateId { get; init; } = string.Empty;
    public long RoundsDelta { get; init; }
    [Required, StringLength(500)] public string Reason { get; init; } = string.Empty;
    [Required, StringLength(80)] public string IdempotencyKey { get; init; } = string.Empty;
}

public sealed record WorkshopOperationContract(
    string Id, WorkshopOperationType OperationType, DateTime EffectiveAt, DateTime RecordedAt,
    string ContributionDigest);

public sealed record WorkshopReconciliationContract(
    string State, int MismatchCount, DateTime LastRunAt, IReadOnlyList<string> Mismatches);

public sealed record WorkshopPageContract<T>(IReadOnlyList<T> Items, int Page, int PageSize, long Total);

public sealed record WorkshopDashboardContract(
    int FirearmCount, int ComponentCount, long TotalFirearmRounds,
    int ComponentsAtOrAboveInterval, long TotalProducedRounds, long AvailableRounds,
    int RecentActivityCount, WorkshopProjectionState ProjectionState);

public sealed record WorkshopAnalyticsBucketContract(
    DateTime Date, long ShootingRounds, long ProducedRounds, int ShootingSessions,
    IReadOnlyList<string> Notes);

public sealed record WorkshopAnalyticsContract(
    DateTime From, DateTime To, long ShootingRounds, int ShootingSessions,
    long TrackedRounds, long UntrackedRounds, long ProducedRounds,
    long AvailableRounds, int MaintenanceEvents, int MalfunctionEvents,
    int CorrectionCount, WorkshopProjectionState ProjectionState,
    IReadOnlyList<WorkshopAnalyticsBucketContract> Buckets);

public sealed record WorkshopReportActivityContract(
    string Id, string OperationId, WorkshopOperationType Type, DateTime EffectiveAt,
    DateTime? RecordedAt, string? Summary, string? EntityLabel);

public sealed record WorkshopReportContract(
    WorkshopAnalyticsContract Analytics,
    IReadOnlyList<WorkshopReportActivityContract> Activities);

public sealed record WorkshopComponentConditionContract(
    ComponentInstance Component, ComponentDefinition Definition, ComponentInstallation Installation);

public sealed record WorkshopRoundHistoryPointContract(
    DateTime EffectiveAt, long Delta, long CumulativeRounds, string OperationId);

public sealed record WorkshopShootingSessionContract(
    ShootingSession Session, bool Superseded);

public sealed record WorkshopShootingSessionReadContract(
    ShootingSession Session, bool Superseded);

public sealed record WorkshopHistoryPageContract<T>(
    IReadOnlyList<T> Items, int Page, int PageSize, bool HasMore);

public sealed record WorkshopFirearmDetailContract(
    Firearm Firearm,
    IReadOnlyList<WorkshopComponentConditionContract> Components,
    WorkshopHistoryPageContract<WorkshopComponentConditionContract> InstallationHistory,
    WorkshopHistoryPageContract<MaintenanceEvent> Maintenance,
    WorkshopHistoryPageContract<WorkshopShootingSessionContract> Shooting,
    IReadOnlyList<WorkshopRoundHistoryPointContract> RoundHistory,
    IReadOnlyList<ComponentInstance> ReplacementCandidates,
    WorkshopProjectionState ProjectionState,
    int MalfunctionCount);
