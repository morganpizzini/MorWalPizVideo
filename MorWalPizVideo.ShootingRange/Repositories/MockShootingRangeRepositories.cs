using System.Collections.Concurrent;
using MorWalPizVideo.ShootingRange.Models;
using MorWalPizVideo.ShootingRange.Security;

namespace MorWalPizVideo.ShootingRange.Repositories;

public class MockShootingRangeRepository<T>(MockShootingRangeRepositorySet owner) : IShootingRangeRepository<T> where T : BaseEntity
{
    protected readonly ConcurrentDictionary<string, T> Items = new();
    internal Action CaptureRollback()
    {
        var snapshot = Items.ToArray();
        return () => { Items.Clear(); foreach (var entry in snapshot) Items[entry.Key] = entry.Value; };
    }
    public Task<T?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var item = Items.GetValueOrDefault(id);
        return Task.FromResult(item is not null && IsVisible(item) ? item : null);
    }

    public Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<T>>(Items.Values.Where(IsVisible).ToArray());
    public Task<T> InsertAsync(T item, CancellationToken cancellationToken = default) => WriteAsync(item, false, cancellationToken);
    public Task<T> ReplaceAsync(T item, CancellationToken cancellationToken = default) => WriteAsync(item, true, cancellationToken);
    protected Task DeleteRecordAsync(string id, CancellationToken cancellationToken = default) => owner.ExecuteGuardedAsync(async () =>
    {
        var existing = await GetAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Record not found.");
        Items.TryRemove(id, out _);
        return true;
    }, cancellationToken);
    private Task<T> WriteAsync(T item, bool replacing, CancellationToken cancellationToken) => owner.ExecuteGuardedAsync(async () =>
    {
        item = ShootingRangeWriteValidation.Prepare(item, owner.TenantContext.TenantId);
        await ShootingRangeWriteValidation.ValidateAsync(owner, item, replacing, cancellationToken);
        if (replacing && Items.TryGetValue(item.Id, out var existing) && !IsVisible(existing))
            throw new KeyNotFoundException("Record not found.");
        if (!replacing && Items.ContainsKey(item.Id)) throw new InvalidOperationException("Duplicate identifier.");
        Items[item.Id] = item;
        return item;
    }, cancellationToken);

    private bool IsVisible(T item) => item is ShootingRangeTenant ||
        owner.TenantContext.TenantId is { } tenantId && item.TenantId == tenantId;
}

public sealed class MockShootingRangeFriendshipRepository(MockShootingRangeRepositorySet owner)
    : MockShootingRangeRepository<ShootingRangeFriendship>(owner), IShootingRangeFriendshipRepository
{
    public Task DeleteFriendshipAsync(string id, CancellationToken cancellationToken = default) =>
        DeleteRecordAsync(id, cancellationToken);
}

public sealed class MockShootingRangeUserRepository(MockShootingRangeRepositorySet owner) : MockShootingRangeRepository<ShootingRangeUser>(owner), IShootingRangeUserRepository
{
    public async Task<ShootingRangeUser?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        (await GetAllAsync(cancellationToken)).FirstOrDefault(user =>
            ShootingRangeWriteValidation.NormalizeUsername(user.Username) == ShootingRangeWriteValidation.NormalizeUsername(username));
}

public sealed class MockShootingRangeBookingRepository(MockShootingRangeRepositorySet owner) : MockShootingRangeRepository<ShootingRangeBooking>(owner), IShootingRangeBookingRepository
{
    public async Task<bool> HasOverlapAsync(string bayId, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default) => (await GetAllAsync(cancellationToken)).Any(item => item.BayId == bayId && item.StartUtc < endUtc && item.EndUtc > startUtc && item.Status is ShootingRangeBookingStatus.Pending or ShootingRangeBookingStatus.Approved);
    public Task<ShootingRangeBooking> InsertIfAvailableAsync(ShootingRangeBooking booking, CancellationToken cancellationToken = default) => InsertAsync(booking, cancellationToken);

    public async Task<(IReadOnlyList<ShootingRangeBooking> Items, int TotalCount)> GetLocalDatePageAsync(
        DateOnly localDate, int offset, int pageSize, CancellationToken cancellationToken = default)
    {
        var matches = (await GetAllAsync(cancellationToken))
            .Where(item => item.LocalDate == localDate)
            .OrderBy(item => item.LocalDate)
            .ThenBy(item => item.StartUtc)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .ToArray();
        return (matches.Skip(offset).Take(pageSize).ToArray(), matches.Length);
    }

    public async Task<(IReadOnlyList<ShootingRangeBooking> Items, int TotalCount)> GetUserReportPageAsync(
        string userId, bool? isNoShow, int offset, int pageSize, CancellationToken cancellationToken = default)
    {
        var matches = (await GetAllAsync(cancellationToken))
            .Where(item => item.UserId == userId && (!isNoShow.HasValue || item.IsNoShow == isNoShow.Value))
            .OrderByDescending(item => item.LocalDate)
            .ThenByDescending(item => item.StartUtc)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .ToArray();
        return (matches.Skip(offset).Take(pageSize).ToArray(), matches.Length);
    }

    public async Task<IReadOnlyList<ShootingRangeBooking>> GetNoShowOccurrencesSinceAsync(
        DateOnly earliestDate, CancellationToken cancellationToken = default) =>
        (await GetAllAsync(cancellationToken))
        .Where(item => item.IsNoShow && item.NoShowDate >= earliestDate)
        .ToArray();
}

public sealed class MockShootingRangeRepositorySet : IShootingRangeRepositorySet
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly AsyncLocal<bool> guarded = new();
    private readonly List<Func<Action>> rollbackFactories = [];
    public TimeProvider Clock { get; }
    public ShootingRangeTenantContext TenantContext { get; }
    public MockShootingRangeRepositorySet(TimeProvider? clock = null, ShootingRangeTenantContext? tenantContext = null)
    {
        Clock = clock ?? TimeProvider.System;
        TenantContext = tenantContext ?? new ShootingRangeTenantContext("test-tenant");
        Configs = Register(new MockShootingRangeRepository<ShootingRangeConfig>(this));
        Tenants = Register(new MockShootingRangeRepository<ShootingRangeTenant>(this));
        Bays = Register(new MockShootingRangeRepository<ShootingRangeBay>(this));
        MechanismTypes = Register(new MockShootingRangeRepository<ShootingRangeMechanismType>(this));
        Exceptions = Register(new MockShootingRangeRepository<ShootingRangeException>(this));
        var users = new MockShootingRangeUserRepository(this);
        Register(users);
        Users = users;
        var bookings = new MockShootingRangeBookingRepository(this);
        Register(bookings);
        Bookings = bookings;
        Threads = Register(new MockShootingRangeRepository<ShootingRangeThread>(this));
        LocalSessions = Register(new MockShootingRangeRepository<ShootingRangeLocalSession>(this));
        var friendships = new MockShootingRangeFriendshipRepository(this);
        Register(friendships);
        Friendships = friendships;
        ((MockShootingRangeRepository<ShootingRangeTenant>)Tenants).InsertAsync(new ShootingRangeTenant
        {
            Id = TenantContext.TenantId ?? "test-tenant",
            Slug = "test-tenant",
            Name = "Test tenant",
            ClientOrigin = "https://range-spa-bjeqb5gwggf0hfaj.westeurope-01.azurewebsites.net"
        }).GetAwaiter().GetResult();
    }
    private MockShootingRangeRepository<T> Register<T>(MockShootingRangeRepository<T> repository) where T : BaseEntity
    {
        rollbackFactories.Add(repository.CaptureRollback);
        return repository;
    }
    public async Task<T> ExecuteGuardedAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        if (guarded.Value) return await action();
        await gate.WaitAsync(cancellationToken);
        var rollbacks = rollbackFactories.Select(factory => factory()).ToArray();
        guarded.Value = true;
        try { return await action(); }
        catch { foreach (var rollback in rollbacks) rollback(); throw; }
        finally { guarded.Value = false; gate.Release(); }
    }
    public IShootingRangeRepository<ShootingRangeConfig> Configs { get; }
    public IShootingRangeRepository<ShootingRangeTenant> Tenants { get; }
    public IShootingRangeRepository<ShootingRangeBay> Bays { get; }
    public IShootingRangeRepository<ShootingRangeMechanismType> MechanismTypes { get; }
    public IShootingRangeRepository<ShootingRangeException> Exceptions { get; }
    public IShootingRangeUserRepository Users { get; }
    public IShootingRangeBookingRepository Bookings { get; }
    public IShootingRangeRepository<ShootingRangeThread> Threads { get; }
    public IShootingRangeRepository<ShootingRangeLocalSession> LocalSessions { get; }
    public IShootingRangeFriendshipRepository Friendships { get; }
}