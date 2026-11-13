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
    [StringLength(4000)] public string? Notes { get; init; }
}

public sealed class UpdateWorkshopFirearmRequest : CreateWorkshopFirearmRequest
{
    [Range(0, long.MaxValue)] public long Revision { get; init; }
    [Required, StringLength(80)] public string IdempotencyKey { get; init; } = string.Empty;
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
}

public sealed class CreateWorkshopComponentRequest
{
    [Required] public string DefinitionId { get; init; } = string.Empty;
    [StringLength(500)] public string? IdentificationNote { get; init; }
    [StringLength(120)] public string? Manufacturer { get; init; }
    [StringLength(120)] public string? Model { get; init; }
    [Range(1, long.MaxValue)] public long? ServiceInterval { get; init; }
    public WorkshopTrackingMode? TrackingMode { get; init; }
    [StringLength(4000)] public string? Notes { get; init; }
}

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
    [StringLength(160)] public string? Drawing { get; init; }
    [StringLength(160)] public string? Primer { get; init; }
    [StringLength(160)] public string? Powder { get; init; }
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

public sealed class RecordWorkshopProductionRequest
{
    [Required] public string RecipeId { get; init; } = string.Empty;
    public DateTime ProductionDate { get; init; }
    [Range(1, long.MaxValue)] public long QuantityProduced { get; init; }
    [StringLength(4000)] public string? Notes { get; init; }
    [Required, StringLength(80)] public string IdempotencyKey { get; init; } = string.Empty;
}

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
    public string? BatchId { get; init; }
    [Range(1, long.MaxValue)] public long Quantity { get; init; }
    public string? AllocationId { get; init; }
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
    public IReadOnlyList<WorkshopConsumptionRequest> ConsumptionRecords { get; init; } = [];
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
    long AllocatedRounds, int RecentActivityCount, WorkshopProjectionState ProjectionState);
