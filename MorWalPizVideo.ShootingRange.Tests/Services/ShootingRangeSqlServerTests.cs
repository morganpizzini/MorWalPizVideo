using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using MorWalPizVideo.ShootingRange.Models;
using MorWalPizVideo.ShootingRange.Repositories;
using MorWalPizVideo.ShootingRange.Security;
using MorWalPizVideo.ShootingRange.Services;

namespace MorWalPizVideo.ShootingRange.Tests.Services;

[Trait("Category", "TestGroup:ShootingRange")]
public sealed class ShootingRangeSqlServerTests
{
    [SqlServerFact]
    public async Task Database_unique_username_constraint_rejects_duplicate_values()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.First.Users.InsertAsync(new() { Id = "second-user", Username = "another", NormalizedUsername = "test-only" });

        var duplicate = async () =>
        {
            fixture.FirstContext.Users.Add(new() { Id = "database-duplicate", Username = "third", NormalizedUsername = "test-only" });
            await fixture.FirstContext.SaveChangesAsync();
        };
        (await duplicate.Should().ThrowAsync<DbUpdateException>()).Which
            .GetBaseException().Should().BeOfType<SqlException>()
            .Which.Number.Should().BeOneOf(2601, 2627);
    }

    [SqlServerFact]
    public async Task Same_normalized_username_is_allowed_in_different_tenants()
    {
        await using var fixture = await Fixture.CreateAsync();
        const string secondTenantId = "second-sql-tenant";
        await fixture.First.Tenants.InsertAsync(new()
        {
            Id = secondTenantId,
            Slug = "second-sql-tenant",
            Name = "Second SQL tenant",
            ClientOrigin = "https://second-sql-tenant.example"
        });

        using (fixture.First.TenantContext.BeginScope(secondTenantId))
            await fixture.First.Users.InsertAsync(new() { Id = "second-user", Username = "test-only" });

        (await fixture.First.Users.FindByUsernameAsync("TEST-ONLY"))!.Id.Should().Be(fixture.UserId);
        using (fixture.First.TenantContext.BeginScope(secondTenantId))
            (await fixture.First.Users.FindByUsernameAsync("TEST-ONLY"))!.Id.Should().Be("second-user");
    }

    [SqlServerFact]
    public async Task Independent_writers_contend_on_guard_and_other_slots_remain_available()
    {
        await using var fixture = await Fixture.CreateAsync();
        async Task<bool> TryBook(SqlServerShootingRangeRepositorySet repositories, string period)
        {
            try
            {
                await new ShootingRangeService(repositories).CreateBookingAsync(fixture.UserId,
                    new(fixture.BayId, fixture.Date, period, ""), default);
                return true;
            }
            catch (InvalidOperationException) { return false; }
        }

        var results = await Task.WhenAll(
            Task.Run(() => TryBook(fixture.First, "morning")),
            Task.Run(() => TryBook(fixture.Second, "MORNING")));
        results.Count(success => success).Should().Be(1);
        (await fixture.First.Bookings.GetAllAsync()).Should().HaveCount(1);

        await new ShootingRangeService(fixture.Second).CreateBookingAsync(fixture.UserId,
            new(fixture.BayId, fixture.Date, "afternoon", ""), default);
        var otherBay = await fixture.Second.Bays.InsertAsync(new() { Id = ShootingRangeIds.NewId(), Code = "02" });
        await new ShootingRangeService(fixture.First).CreateBookingAsync(fixture.UserId,
            new(otherBay.Id, fixture.Date, "morning", ""), default);
    }

    [SqlServerFact]
    public async Task Guarded_failure_rolls_back_multiple_repository_mutations()
    {
        await using var fixture = await Fixture.CreateAsync();
        var booking = await new ShootingRangeService(fixture.First).CreateBookingAsync(fixture.UserId,
            new(fixture.BayId, fixture.Date, "morning", ""), default);
        var action = () => fixture.First.ExecuteGuardedAsync<bool>(async () =>
        {
            var current = await fixture.First.Bookings.GetAsync(booking.Id);
            await fixture.First.Bookings.ReplaceAsync(current! with { Status = ShootingRangeBookingStatus.Cancelled });
            var bay = await fixture.First.Bays.GetAsync(fixture.BayId);
            await fixture.First.Bays.ReplaceAsync(bay! with { Status = ShootingRangeBayStatus.Maintenance });
            throw new ArgumentException("rollback probe");
        });

        await action.Should().ThrowAsync<ArgumentException>();
        (await fixture.Second.Bookings.GetAsync(booking.Id)).Should().Be(booking);
        (await fixture.Second.Bays.GetAsync(fixture.BayId))!.Status.Should().Be(ShootingRangeBayStatus.Available);
    }

    [SqlServerFact]
    public async Task Historical_period_is_not_reinterpreted_after_schedule_change()
    {
        await using var fixture = await Fixture.CreateAsync();
        var historical = await new ShootingRangeService(fixture.First).CreateBookingAsync(fixture.UserId,
            new(fixture.BayId, fixture.Date, "morning", ""), default);
        await fixture.Second.Configs.ReplaceAsync(new() { Id = "default", SessionMode = ShootingRangeSessionMode.Hourly });

        var create = () => new ShootingRangeService(fixture.Second).CreateBookingAsync(fixture.UserId,
            new(fixture.BayId, fixture.Date, "10:00", ""), default);
        await create.Should().ThrowAsync<InvalidOperationException>();
        (await fixture.First.Bookings.GetAsync(historical.Id)).Should().Be(historical);
    }

    [SqlServerFact]
    public async Task Independent_reactivations_have_one_winner()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (start, end) = ShootingRangeService.ResolvePeriod(new(), fixture.Date, "morning");
        var first = await fixture.First.Bookings.InsertAsync(new()
        {
            Id = ShootingRangeIds.NewId(), UserId = fixture.UserId, BayId = fixture.BayId, LocalDate = fixture.Date,
            PeriodKey = "morning", StartUtc = start, EndUtc = end, Status = ShootingRangeBookingStatus.Rejected
        });
        var second = await fixture.Second.Bookings.InsertAsync(first with
        {
            Id = ShootingRangeIds.NewId(), PeriodKey = "MORNING", Status = ShootingRangeBookingStatus.Cancelled
        });

        async Task<bool> TryActivate(SqlServerShootingRangeRepositorySet repositories, ShootingRangeBooking booking)
        {
            try
            {
                await repositories.Bookings.ReplaceAsync(booking with { Status = ShootingRangeBookingStatus.Approved });
                return true;
            }
            catch (InvalidOperationException) { return false; }
        }

        var results = await Task.WhenAll(
            Task.Run(() => TryActivate(fixture.First, first)),
            Task.Run(() => TryActivate(fixture.Second, second)));
        results.Count(success => success).Should().Be(1);
        (await fixture.First.Bookings.GetAllAsync()).Count(booking =>
            booking.Status is ShootingRangeBookingStatus.Pending or ShootingRangeBookingStatus.Approved).Should().Be(1);
    }

    [SqlServerFact]
    public async Task Approval_and_rejection_preserve_interval_and_capacity()
    {
        await using var fixture = await Fixture.CreateAsync();
        var booking = await new ShootingRangeService(fixture.First).CreateBookingAsync(fixture.UserId,
            new(fixture.BayId, fixture.Date, "morning", ""), default);
        await Task.WhenAll(
            Task.Run(() => fixture.First.Bookings.ReplaceAsync(booking with { Status = ShootingRangeBookingStatus.Approved })),
            Task.Run(() => fixture.Second.Bookings.ReplaceAsync(booking with { Status = ShootingRangeBookingStatus.Rejected })));

        var current = (await fixture.First.Bookings.GetAsync(booking.Id))!;
        current.StartUtc.Should().Be(booking.StartUtc);
        current.EndUtc.Should().Be(booking.EndUtc);
        current.StartUtc.Kind.Should().Be(DateTimeKind.Utc);
        current.EndUtc.Kind.Should().Be(DateTimeKind.Utc);
        current.Status.Should().BeOneOf(ShootingRangeBookingStatus.Approved, ShootingRangeBookingStatus.Rejected);
        var create = () => new ShootingRangeService(fixture.Second).CreateBookingAsync(fixture.UserId,
            new(fixture.BayId, fixture.Date, "MORNING", ""), default);
        if (current.Status == ShootingRangeBookingStatus.Approved)
            await create.Should().ThrowAsync<InvalidOperationException>();
        else
            await create();
        (await fixture.First.Bookings.GetAllAsync()).Count(item =>
            item.Status is ShootingRangeBookingStatus.Pending or ShootingRangeBookingStatus.Approved).Should().Be(1);
    }

    [SqlServerFact]
    public async Task Concurrent_thread_reply_and_status_updates_preserve_both_changes()
    {
        await using var fixture = await Fixture.CreateAsync();
        const string adminId = "message-admin";
        await fixture.First.Users.InsertAsync(new()
        {
            Id = adminId,
            Username = adminId,
            Status = ShootingRangeAccountStatus.Approved,
            IsAdmin = true
        });
        var thread = await fixture.First.Threads.InsertAsync(new()
        {
            Id = "message-thread",
            UserId = fixture.UserId,
            Subject = "Question",
            Messages = [new(fixture.UserId, false, "Original", DateTime.UtcNow)]
        });

        await Task.WhenAll(
            new ShootingRangeService(fixture.First).ReplyToThreadAsync(adminId, thread.Id, "Reply", default),
            new ShootingRangeService(fixture.Second).DecideThreadStatusAsync(
                adminId, thread.Id, ShootingRangeThreadStatus.Completed, default));

        var persisted = (await fixture.First.Threads.GetAsync(thread.Id))!;
        persisted.Status.Should().Be(ShootingRangeThreadStatus.Completed);
        persisted.Messages.Select(message => message.Text).Should().Contain("Original").And.Contain("Reply");
    }

    private sealed class SqlServerFactAttribute : FactAttribute
    {
        public SqlServerFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SHOOTING_RANGE_TEST_SQL_HOST")))
                Skip = "Requires an explicitly configured disposable SQL Server test instance.";
        }
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly string adminConnectionString;
        private readonly string databaseName;
        private readonly string databaseConnectionString;
        private readonly ShootingRangeTenantContext tenantContext = new("sql-test-tenant");

        public ShootingRangeDbContext FirstContext { get; }
        private ShootingRangeDbContext SecondContext { get; }
        public SqlServerShootingRangeRepositorySet First { get; }
        public SqlServerShootingRangeRepositorySet Second { get; }
        public string UserId { get; } = ShootingRangeIds.NewId();
        public string BayId { get; } = ShootingRangeIds.NewId();
        public DateOnly Date { get; }

        private Fixture(string adminConnectionString, string databaseName, string databaseConnectionString)
        {
            this.adminConnectionString = adminConnectionString;
            this.databaseName = databaseName;
            this.databaseConnectionString = databaseConnectionString;
            var options = new DbContextOptionsBuilder<ShootingRangeDbContext>()
                .UseSqlServer(databaseConnectionString)
                .Options;
            FirstContext = new(options, tenantContext);
            SecondContext = new(options, tenantContext);
            First = new(FirstContext);
            Second = new(SecondContext);
            var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14));
            while (date.DayOfWeek != DayOfWeek.Saturday) date = date.AddDays(1);
            Date = date;
        }

        public static async Task<Fixture> CreateAsync()
        {
            var adminHost = Environment.GetEnvironmentVariable("SHOOTING_RANGE_TEST_SQL_HOST")
                ?? throw new InvalidOperationException("SQL Server test instance must be explicitly configured.");
            var adminBuilder = new SqlConnectionStringBuilder
            {
                DataSource = $"{adminHost},{(int.TryParse(Environment.GetEnvironmentVariable("SHOOTING_RANGE_TEST_SQL_PORT"), out var port) ? port : 1433)}",
                InitialCatalog = Environment.GetEnvironmentVariable("SHOOTING_RANGE_TEST_SQL_DATABASE") ?? "master",
                UserID = Environment.GetEnvironmentVariable("SHOOTING_RANGE_TEST_SQL_USERNAME") ?? "sa",
                Password = Environment.GetEnvironmentVariable("SHOOTING_RANGE_TEST_SQL_PASSWORD"),
                Encrypt = SqlConnectionEncryptOption.Mandatory,
                TrustServerCertificate = true,
                Pooling = false
            };
            var databaseName = $"shooting_range_test_{Guid.NewGuid():N}";
            await using (var connection = new SqlConnection(adminBuilder.ConnectionString))
            {
                await connection.OpenAsync();
                await using var command = new SqlCommand($"CREATE DATABASE [{databaseName}]", connection);
                await command.ExecuteNonQueryAsync();
            }

            var databaseBuilder = new SqlConnectionStringBuilder(adminBuilder.ConnectionString)
            {
                InitialCatalog = databaseName,
                Pooling = false
            };
            var fixture = new Fixture(adminBuilder.ConnectionString, databaseName, databaseBuilder.ConnectionString);
            await fixture.FirstContext.Database.MigrateAsync();
            await fixture.First.Tenants.InsertAsync(new()
            {
                Id = "sql-test-tenant",
                Slug = "sql-test-tenant",
                Name = "SQL test tenant",
                ClientOrigin = "https://sql-test-tenant.example"
            });
            (await fixture.First.Configs.GetAllAsync()).Should().BeEmpty();
            await fixture.First.Configs.ReplaceAsync(new() { Id = "default" });
            await fixture.First.Users.InsertAsync(new() { Id = fixture.UserId, Username = "test-only", Status = ShootingRangeAccountStatus.Approved });
            await fixture.First.Bays.InsertAsync(new() { Id = fixture.BayId, Code = "01" });
            return fixture;
        }

        public async ValueTask DisposeAsync()
        {
            await FirstContext.DisposeAsync();
            await SecondContext.DisposeAsync();
            await using var connection = new SqlConnection(adminConnectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand(
                $"IF DB_ID(N'{databaseName}') IS NOT NULL BEGIN ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{databaseName}]; END",
                connection);
            await command.ExecuteNonQueryAsync();
        }
    }
}
