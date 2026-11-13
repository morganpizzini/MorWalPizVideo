using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MorWalPizVideo.Models.Models.Workshop;

namespace MorWalPizVideo.Domain.Workshop;

public sealed record WorkshopReplayValue(
    WorkshopAggregateKind Kind, string Id, long Rounds, long Stock, long Allocated,
    long OriginalAllocation, long ConsumedAllocation, long ReleasedAllocation);
public sealed record WorkshopReplayResult(IReadOnlyList<WorkshopReplayValue> Values, string Digest)
{
    public WorkshopReplayValue For(WorkshopAggregateKind kind, string id) =>
        Values.FirstOrDefault(x => x.Kind == kind && x.Id == id) ?? new(kind, id, 0, 0, 0, 0, 0, 0);
}

public static class WorkshopReplay
{
    public static WorkshopReplayResult Rebuild(IEnumerable<WorkshopOperation> operations)
    {
        var values = operations
            .SelectMany(operation => operation.Deltas)
            .GroupBy(delta => (delta.AggregateKind, delta.AggregateId))
            .Select(group => new WorkshopReplayValue(
                group.Key.AggregateKind, group.Key.AggregateId,
                group.Sum(x => x.RoundsDelta), group.Sum(x => x.StockDelta), group.Sum(x => x.AllocationDelta),
                group.Where(x => x.AllocationDelta > 0).Sum(x => x.AllocationDelta),
                group.Sum(x => x.AllocationConsumedDelta), group.Sum(x => x.AllocationReleasedDelta)))
            .OrderBy(x => x.Kind).ThenBy(x => x.Id).ToArray();
        return new WorkshopReplayResult(values, Digest(values));
    }

    public static string Digest(IEnumerable<WorkshopDelta> deltas)
    {
        var canonical = deltas.OrderBy(x => x.AggregateKind).ThenBy(x => x.AggregateId)
            .ThenBy(x => x.RoundsDelta).ThenBy(x => x.StockDelta).ThenBy(x => x.AllocationDelta)
            .ThenBy(x => x.AllocationConsumedDelta).ThenBy(x => x.AllocationReleasedDelta)
            .ToArray();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(canonical))))
            .ToLowerInvariant();
    }

    public static string Digest(IEnumerable<WorkshopReplayValue> values) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(values)))).ToLowerInvariant();
}

public sealed record WorkshopReconciliationResult(
    WorkshopProjectionState State, IReadOnlyList<string> Mismatches, string Digest, DateTime CompletedAt);

public sealed class WorkshopReconciliationService(
    IWorkshopRepository repository,
    TimeProvider timeProvider)
{
    public async Task<WorkshopReconciliationResult> ReconcileAsync(
        string accountId, bool repair, CancellationToken cancellationToken = default)
    {
        var operations = await repository.ListAsync<WorkshopOperation>(
            MorWalPizVideo.Models.Constraints.DbCollections.WorkshopOperations, accountId, cancellationToken);
        var replay = WorkshopReplay.Rebuild(operations);
        var mismatches = new List<string>();

        await CompareAsync<Firearm>(MorWalPizVideo.Models.Constraints.DbCollections.WorkshopFirearms,
            WorkshopAggregateKind.Firearm, (x, value) => x.TotalRounds != value.Rounds,
            (x, value) => x with { TotalRounds = value.Rounds, Revision = x.Revision + 1 }, replay, mismatches, repair, accountId, cancellationToken);
        await CompareAsync<ComponentInstance>(MorWalPizVideo.Models.Constraints.DbCollections.WorkshopComponentInstances,
            WorkshopAggregateKind.Component, (x, value) => x.TotalRounds != value.Rounds,
            (x, value) => x with { TotalRounds = value.Rounds, Revision = x.Revision + 1 }, replay, mismatches, repair, accountId, cancellationToken);
        await CompareAsync<AmmoBatch>(MorWalPizVideo.Models.Constraints.DbCollections.WorkshopAmmoBatches,
            WorkshopAggregateKind.Batch, (x, value) => x.OnHandQuantity != value.Stock,
            (x, value) => x with { OnHandQuantity = value.Stock, Revision = x.Revision + 1 }, replay, mismatches, repair, accountId, cancellationToken);
        await CompareAsync<AmmoAllocation>(MorWalPizVideo.Models.Constraints.DbCollections.WorkshopAmmoAllocations,
            WorkshopAggregateKind.Allocation, (x, value) => x.ActiveQuantity != value.Allocated ||
                x.Quantity != value.OriginalAllocation || x.ConsumedQuantity != value.ConsumedAllocation ||
                x.ReleasedQuantity != value.ReleasedAllocation,
            (x, value) => x with
            {
                Quantity = value.OriginalAllocation,
                ConsumedQuantity = value.ConsumedAllocation,
                ReleasedQuantity = value.ReleasedAllocation,
                Revision = x.Revision + 1
            }, replay, mismatches, repair, accountId, cancellationToken);

        if (repair && mismatches.Count > 0)
            return await ReconcileAsync(accountId, false, cancellationToken);

        var state = mismatches.Count == 0 ? WorkshopProjectionState.Current :
            repair ? WorkshopProjectionState.Pending : WorkshopProjectionState.Inconsistent;
        var completedAt = timeProvider.GetUtcNow().UtcDateTime;
        await repository.SaveReconciliationAsync(new WorkshopReconciliationState
        {
            Id = accountId,
            AccountId = accountId,
            State = state,
            MismatchCount = mismatches.Count,
            Mismatches = mismatches,
            LastRunAt = completedAt
        }, cancellationToken);
        return new(state, mismatches, replay.Digest, completedAt);
    }

    private async Task CompareAsync<T>(
        string collection, WorkshopAggregateKind kind,
        Func<T, WorkshopReplayValue, bool> differs,
        Func<T, WorkshopReplayValue, T> repair,
        WorkshopReplayResult replay, List<string> mismatches, bool repairEnabled, string accountId,
        CancellationToken cancellationToken) where T : MorWalPizVideo.Server.Models.BaseEntity
    {
        var current = await repository.ListAsync<T>(collection, accountId, cancellationToken);
        foreach (var item in current)
        {
            var id = item.Id;
            var value = replay.For(kind, id);
            if (!differs(item, value)) continue;
            mismatches.Add($"{kind}:{id}");
            if (repairEnabled)
                await repository.ReplaceAsync(collection, repair(item, value), accountId, cancellationToken: cancellationToken);
        }
    }
}
