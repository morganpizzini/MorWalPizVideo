using System.Collections.Concurrent;
using MorWalPizVideo.ShootingRange.Models;

namespace MorWalPizVideo.ShootingRange.Repositories;

public class MockShootingRangeRepository<T>(MockShootingRangeRepositorySet owner) : IShootingRangeRepository<T> where T : BaseEntity
{
    protected readonly ConcurrentDictionary<string, T> Items = new();
    internal Action CaptureRollback()
    {
        var snapshot = Items.ToArray();
        return () => { Items.Clear(); foreach (var entry in snapshot) Items[entry.Key] = entry.Value; };
    }
    public Task<T?> GetAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult(Items.GetValueOrDefault(id));
    public Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<T>>(Items.Values.ToList());
    public Task<T> InsertAsync(T item, CancellationToken cancellationToken = default) => WriteAsync(item, false, cancellationToken);
    public Task<T> ReplaceAsync(T item, CancellationToken cancellationToken = default) => WriteAsync(item, true, cancellationToken);
    private Task<T> WriteAsync(T item, bool replacing, CancellationToken cancellationToken) => owner.ExecuteGuardedAsync(async () =>
    {
        item = ShootingRangeWriteValidation.Prepare(item);
        await ShootingRangeWriteValidation.ValidateAsync(owner, item, replacing, cancellationToken);
        if (!replacing && Items.ContainsKey(item.Id)) throw new InvalidOperationException("Duplicate identifier.");
        Items[item.Id] = item;
        return item;
    }, cancellationToken);
}

public sealed class MockShootingRangeUserRepository(MockShootingRangeRepositorySet owner) : MockShootingRangeRepository<ShootingRangeUser>(owner), IShootingRangeUserRepository
{
    public Task<ShootingRangeUser?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default) => Task.FromResult(Items.Values.FirstOrDefault(user => ShootingRangeWriteValidation.NormalizeUsername(user.Username) == ShootingRangeWriteValidation.NormalizeUsername(username)));
}

public sealed class MockShootingRangeBookingRepository(MockShootingRangeRepositorySet owner) : MockShootingRangeRepository<ShootingRangeBooking>(owner), IShootingRangeBookingRepository
{
    public async Task<bool> HasOverlapAsync(string bayId, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default) => (await GetAllAsync(cancellationToken)).Any(item => item.BayId == bayId && item.StartUtc < endUtc && item.EndUtc > startUtc && item.Status is ShootingRangeBookingStatus.Pending or ShootingRangeBookingStatus.Approved);
    public Task<ShootingRangeBooking> InsertIfAvailableAsync(ShootingRangeBooking booking, CancellationToken cancellationToken = default) => InsertAsync(booking, cancellationToken);
}

public sealed class MockShootingRangeRepositorySet : IShootingRangeRepositorySet
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly AsyncLocal<bool> guarded = new();
    private readonly List<Func<Action>> rollbackFactories = [];
    public TimeProvider Clock { get; }
    public MockShootingRangeRepositorySet(TimeProvider? clock = null)
    {
        Clock = clock ?? TimeProvider.System;
        Configs = Register(new MockShootingRangeRepository<ShootingRangeConfig>(this));
        Bays = Register(new MockShootingRangeRepository<ShootingRangeBay>(this));
        Exceptions = Register(new MockShootingRangeRepository<ShootingRangeException>(this));
        var users = new MockShootingRangeUserRepository(this);
        Register(users);
        Users = users;
        var bookings = new MockShootingRangeBookingRepository(this);
        Register(bookings);
        Bookings = bookings;
        Threads = Register(new MockShootingRangeRepository<ShootingRangeThread>(this));
        LocalSessions = Register(new MockShootingRangeRepository<ShootingRangeLocalSession>(this));
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
    public IShootingRangeRepository<ShootingRangeBay> Bays { get; }
    public IShootingRangeRepository<ShootingRangeException> Exceptions { get; }
    public IShootingRangeUserRepository Users { get; }
    public IShootingRangeBookingRepository Bookings { get; }
    public IShootingRangeRepository<ShootingRangeThread> Threads { get; }
    public IShootingRangeRepository<ShootingRangeLocalSession> LocalSessions { get; }
}