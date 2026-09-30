using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using MorWalPizVideo.ShootingRange.Models;
using MorWalPizVideo.ShootingRange.Repositories;
using MorWalPizVideo.ShootingRange.Services;

namespace MorWalPizVideo.BackOffice.Tests.Services;

public sealed class ShootingRangeMongoTests
{
    [MongoFact]
    public async Task Normalized_username_index_rejects_writes_outside_the_guard_and_sessions_persist()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.First.VerifySecurityIndexesAsync();
        var duplicate = new ShootingRangeUser { Id = ObjectId.GenerateNewId().ToString(), Username = " TEST-ONLY ", NormalizedUsername = "test-only" };
        var insert = () => fixture.Database.GetCollection<ShootingRangeUser>("shootingRangeUsers").InsertOneAsync(duplicate);
        (await insert.Should().ThrowAsync<MongoWriteException>()).Which.WriteError.Category.Should().Be(ServerErrorCategory.DuplicateKey);
        var session = await fixture.First.LocalSessions.InsertAsync(new() { Id = new string('B', 64), UserId = fixture.UserId, ExpiresUtc = DateTime.UtcNow.AddHours(1) });
        (await fixture.Second.LocalSessions.GetAsync(session.Id))!.UserId.Should().Be(fixture.UserId);
        await fixture.Second.LocalSessions.ReplaceAsync(session with { Revoked = true });
        (await fixture.First.LocalSessions.GetAsync(session.Id))!.Revoked.Should().BeTrue();
    }

    [MongoFact]
    public async Task Independent_writers_contend_on_guard_not_period_index()
    {
        await using var fixture = await Fixture.CreateAsync();
        async Task<bool> TryBook(MongoShootingRangeRepositorySet repositories, string key)
        {
            try { await new ShootingRangeService(repositories).CreateBookingAsync(fixture.UserId, new(fixture.BayId, fixture.Date, key, ""), default); return true; }
            catch (InvalidOperationException) { return false; }
        }
        var results = await Task.WhenAll(Task.Run(() => TryBook(fixture.First, "morning")), Task.Run(() => TryBook(fixture.Second, "MORNING")));
        results.Count(success => success).Should().Be(1);
        (await fixture.First.Bookings.GetAllAsync()).Should().HaveCount(1);
        await new ShootingRangeService(fixture.Second).CreateBookingAsync(fixture.UserId, new(fixture.BayId, fixture.Date, "afternoon", ""), default);
        var otherBay = await fixture.Second.Bays.InsertAsync(new() { Id = ObjectId.GenerateNewId().ToString(), Code = "02" });
        await new ShootingRangeService(fixture.First).CreateBookingAsync(fixture.UserId, new(otherBay.Id, fixture.Date, "morning", ""), default);
    }

    [MongoFact]
    public async Task Reactivation_competes_with_creation_and_failed_mutation_rolls_back()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (start, end) = ShootingRangeService.ResolvePeriod(new(), fixture.Date, "morning");
        var inactive = await fixture.First.Bookings.InsertAsync(new() { Id = ObjectId.GenerateNewId().ToString(), UserId = fixture.UserId, BayId = fixture.BayId, LocalDate = fixture.Date, PeriodKey = "MORNING", StartUtc = start, EndUtc = end, Status = ShootingRangeBookingStatus.Rejected });
        async Task<bool> TryActivate()
        {
            try { await fixture.First.Bookings.ReplaceAsync(inactive with { Status = ShootingRangeBookingStatus.Approved }); return true; }
            catch (InvalidOperationException) { return false; }
        }
        async Task<bool> TryCreate()
        {
            try { await new ShootingRangeService(fixture.Second).CreateBookingAsync(fixture.UserId, new(fixture.BayId, fixture.Date, "morning", ""), default); return true; }
            catch (InvalidOperationException) { return false; }
        }
        (await Task.WhenAll(Task.Run(TryActivate), Task.Run(TryCreate))).Count(success => success).Should().Be(1);
        (await fixture.First.Bookings.GetAllAsync()).Count(booking => booking.Status is ShootingRangeBookingStatus.Pending or ShootingRangeBookingStatus.Approved).Should().Be(1);
        var action = () => fixture.First.ExecuteGuardedAsync<bool>(async () =>
        {
            var current = await fixture.First.Bookings.GetAsync(inactive.Id);
            await fixture.First.Bookings.ReplaceAsync(current! with { Status = ShootingRangeBookingStatus.Cancelled });
            await fixture.First.Bays.ReplaceAsync((await fixture.First.Bays.GetAsync(fixture.BayId))! with { Status = ShootingRangeBayStatus.Maintenance });
            throw new ArgumentException("rollback probe");
        });
        var before = await fixture.First.Bookings.GetAsync(inactive.Id);
        await action.Should().ThrowAsync<ArgumentException>();
        (await fixture.Second.Bookings.GetAsync(inactive.Id)).Should().Be(before);
        (await fixture.Second.Bays.GetAsync(fixture.BayId))!.Status.Should().Be(ShootingRangeBayStatus.Available);
    }

    [MongoFact]
    public async Task Historical_period_blocks_hourly_slots_without_reinterpreting_interval()
    {
        await using var fixture = await Fixture.CreateAsync();
        var historical = await new ShootingRangeService(fixture.First).CreateBookingAsync(fixture.UserId, new(fixture.BayId, fixture.Date, "morning", ""), default);
        await fixture.Second.Configs.ReplaceAsync(new() { Id = "default", SessionMode = ShootingRangeSessionMode.Hourly });
        var action = () => new ShootingRangeService(fixture.Second).CreateBookingAsync(fixture.UserId, new(fixture.BayId, fixture.Date, "10:00", ""), default);
        await action.Should().ThrowAsync<InvalidOperationException>();
        (await fixture.First.Bookings.GetAsync(historical.Id)).Should().Be(historical);
    }

    [MongoFact]
    public async Task Independent_reactivations_have_one_winner()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (start, end) = ShootingRangeService.ResolvePeriod(new(), fixture.Date, "morning");
        var first = await fixture.First.Bookings.InsertAsync(new() { Id = ObjectId.GenerateNewId().ToString(), UserId = fixture.UserId, BayId = fixture.BayId, LocalDate = fixture.Date, PeriodKey = "morning", StartUtc = start, EndUtc = end, Status = ShootingRangeBookingStatus.Rejected });
        var second = await fixture.Second.Bookings.InsertAsync(first with { Id = ObjectId.GenerateNewId().ToString(), PeriodKey = "MORNING", Status = ShootingRangeBookingStatus.Cancelled });
        async Task<bool> TryActivate(MongoShootingRangeRepositorySet repositories, ShootingRangeBooking booking)
        {
            try { await repositories.Bookings.ReplaceAsync(booking with { Status = ShootingRangeBookingStatus.Approved }); return true; }
            catch (InvalidOperationException) { return false; }
        }
        var results = await Task.WhenAll(Task.Run(() => TryActivate(fixture.First, first)), Task.Run(() => TryActivate(fixture.Second, second)));
        results.Count(success => success).Should().Be(1);
        (await fixture.First.Bookings.GetAllAsync()).Count(booking => booking.Status is ShootingRangeBookingStatus.Pending or ShootingRangeBookingStatus.Approved).Should().Be(1);
    }

    [MongoFact]
    public async Task Approval_and_rejection_race_preserves_interval_and_capacity()
    {
        await using var fixture = await Fixture.CreateAsync();
        var booking = await new ShootingRangeService(fixture.First).CreateBookingAsync(fixture.UserId, new(fixture.BayId, fixture.Date, "morning", ""), default);
        await Task.WhenAll(
            Task.Run(() => fixture.First.Bookings.ReplaceAsync(booking with { Status = ShootingRangeBookingStatus.Approved })),
            Task.Run(() => fixture.Second.Bookings.ReplaceAsync(booking with { Status = ShootingRangeBookingStatus.Rejected })));
        var current = (await fixture.First.Bookings.GetAsync(booking.Id))!;
        current.StartUtc.Should().Be(booking.StartUtc);
        current.EndUtc.Should().Be(booking.EndUtc);
        current.Status.Should().BeOneOf(ShootingRangeBookingStatus.Approved, ShootingRangeBookingStatus.Rejected);
        var create = () => new ShootingRangeService(fixture.Second).CreateBookingAsync(fixture.UserId, new(fixture.BayId, fixture.Date, "MORNING", ""), default);
        if (current.Status == ShootingRangeBookingStatus.Approved) await create.Should().ThrowAsync<InvalidOperationException>();
        else await create();
        (await fixture.First.Bookings.GetAllAsync()).Count(item => item.Status is ShootingRangeBookingStatus.Pending or ShootingRangeBookingStatus.Approved).Should().Be(1);
    }

    private sealed class MongoFactAttribute : FactAttribute
    {
        public MongoFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SHOOTING_RANGE_TEST_MONGO_URI")))
                Skip = "Requires an explicitly configured disposable transaction-capable Mongo test server.";
        }
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly IMongoDatabase database;
        public IMongoDatabase Database => database;
        public MongoShootingRangeRepositorySet First { get; }
        public MongoShootingRangeRepositorySet Second { get; }
        public string UserId { get; } = ObjectId.GenerateNewId().ToString();
        public string BayId { get; } = ObjectId.GenerateNewId().ToString();
        public DateOnly Date { get; }
        private Fixture(IMongoClient firstClient, IMongoClient secondClient, string name)
        {
            database = firstClient.GetDatabase(name);
            First = new(database);
            Second = new(secondClient.GetDatabase(name));
            var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14));
            while (date.DayOfWeek != DayOfWeek.Saturday) date = date.AddDays(1);
            Date = date;
        }
        public static async Task<Fixture> CreateAsync()
        {
            var uri = Environment.GetEnvironmentVariable("SHOOTING_RANGE_TEST_MONGO_URI") ?? throw new InvalidOperationException("Test Mongo must be explicit.");
            var fixture = new Fixture(new MongoClient(uri), new MongoClient(uri), $"shooting_range_test_{Guid.NewGuid():N}");
            await fixture.First.VerifyTransactionsAsync();
            (await fixture.First.Configs.GetAllAsync()).Should().BeEmpty();
            await fixture.First.Configs.ReplaceAsync(new() { Id = "default" });
            await fixture.First.Users.InsertAsync(new() { Id = fixture.UserId, Username = "test-only", Status = ShootingRangeAccountStatus.Approved });
            await fixture.First.Bays.InsertAsync(new() { Id = fixture.BayId, Code = "01" });
            return fixture;
        }
        public async ValueTask DisposeAsync()
        {
            foreach (var name in new[] { "shootingRangeConfigs", "shootingRangeUsers", "shootingRangeBays", "shootingRangeBookings", "shootingRangeMutationGuard", "shootingRangeLocalSessions" })
                await database.GetCollection<BsonDocument>(name).DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
        }
    }
}