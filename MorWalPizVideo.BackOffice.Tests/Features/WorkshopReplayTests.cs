using MorWalPizVideo.Domain.Workshop;
using MorWalPizVideo.Models.Models.Workshop;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class WorkshopReplayTests
{
    [Fact]
    public void Delta_digest_is_deterministic_without_recursive_overload_resolution()
    {
        var first = new WorkshopDelta(WorkshopAggregateKind.Firearm, "firearm-1", RoundsDelta: 25);
        var second = new WorkshopDelta(WorkshopAggregateKind.Component, "component-1", RoundsDelta: 25);

        var forward = WorkshopReplay.Digest([first, second]);
        var reverse = WorkshopReplay.Digest([second, first]);

        Assert.Equal(forward, reverse);
        Assert.False(string.IsNullOrWhiteSpace(forward));
    }

    [Fact]
    public void Replay_rebuilds_counter_families_independently()
    {
        var operations = new[]
        {
            Operation("shooting-1",
                new WorkshopDelta(WorkshopAggregateKind.Firearm, "firearm-1", RoundsDelta: 100),
                new WorkshopDelta(WorkshopAggregateKind.Component, "component-1", RoundsDelta: 100),
                new WorkshopDelta(WorkshopAggregateKind.Batch, "batch-1", StockDelta: -40),
                new WorkshopDelta(WorkshopAggregateKind.Allocation, "allocation-1", AllocationDelta: -40,
                    AllocationConsumedDelta: 40)),
            Operation("allocation-1",
                new WorkshopDelta(WorkshopAggregateKind.Allocation, "allocation-1", AllocationDelta: 100)),
            Operation("production-1",
                new WorkshopDelta(WorkshopAggregateKind.Batch, "batch-1", StockDelta: 200)),
            Operation("counter-adjustment",
                new WorkshopDelta(WorkshopAggregateKind.Component, "component-1", RoundsDelta: 5))
        };

        var result = WorkshopReplay.Rebuild(operations);

        Assert.Equal(100, result.For(WorkshopAggregateKind.Firearm, "firearm-1").Rounds);
        Assert.Equal(105, result.For(WorkshopAggregateKind.Component, "component-1").Rounds);
        Assert.Equal(160, result.For(WorkshopAggregateKind.Batch, "batch-1").Stock);
        Assert.Equal(60, result.For(WorkshopAggregateKind.Allocation, "allocation-1").Allocated);
        Assert.Equal(100, result.For(WorkshopAggregateKind.Allocation, "allocation-1").OriginalAllocation);
        Assert.Equal(40, result.For(WorkshopAggregateKind.Allocation, "allocation-1").ConsumedAllocation);
    }

    [Fact]
    public void Correction_reversal_restores_every_affected_projection_family()
    {
        var original = Operation("original",
            new WorkshopDelta(WorkshopAggregateKind.Firearm, "firearm-1", RoundsDelta: 50),
            new WorkshopDelta(WorkshopAggregateKind.Component, "component-1", RoundsDelta: 50),
            new WorkshopDelta(WorkshopAggregateKind.Batch, "batch-1", StockDelta: -50));
        var reversal = Operation("reversal",
            new WorkshopDelta(WorkshopAggregateKind.Firearm, "firearm-1", RoundsDelta: -50),
            new WorkshopDelta(WorkshopAggregateKind.Component, "component-1", RoundsDelta: -50),
            new WorkshopDelta(WorkshopAggregateKind.Batch, "batch-1", StockDelta: 50));

        var result = WorkshopReplay.Rebuild(new[] { original, reversal });

        Assert.All(result.Values, value =>
        {
            Assert.Equal(0, value.Rounds);
            Assert.Equal(0, value.Stock);
            Assert.Equal(0, value.Allocated);
        });
    }

    private static WorkshopOperation Operation(string id, params WorkshopDelta[] deltas) =>
        new()
        {
            Id = id,
            AccountId = "account-1",
            ClientIdempotencyKey = id,
            OperationType = WorkshopOperationType.ShootingRecorded,
            Deltas = deltas,
            ContributionDigest = WorkshopReplay.Digest(deltas)
        };
}
