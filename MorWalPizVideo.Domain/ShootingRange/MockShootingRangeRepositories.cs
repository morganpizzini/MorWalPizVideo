using System.Collections.Concurrent;
using MorWalPizVideo.Models.ShootingRange;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Domain.ShootingRange;

public class MockShootingRangeRepository<T> : IShootingRangeRepository<T> where T : BaseEntity
{
    protected readonly ConcurrentDictionary<string, T> Items = new();
    public Task<T?> GetAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult(Items.GetValueOrDefault(id));
    public Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<T>>(Items.Values.ToList());
    public Task<T> InsertAsync(T item, CancellationToken cancellationToken = default) { Items[item.Id] = item; return Task.FromResult(item); }
    public Task<T> ReplaceAsync(T item, CancellationToken cancellationToken = default) { Items[item.Id] = item; return Task.FromResult(item); }
}
public sealed class MockShootingRangeUserRepository : MockShootingRangeRepository<ShootingRangeUser>, IShootingRangeUserRepository
{ public Task<ShootingRangeUser?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default) => Task.FromResult(Items.Values.FirstOrDefault(x => x.Username == username)); }
public sealed class MockShootingRangeBookingRepository : MockShootingRangeRepository<ShootingRangeBooking>, IShootingRangeBookingRepository
{ private readonly SemaphoreSlim gate = new(1, 1); public async Task<bool> HasOverlapAsync(string bayId, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default) { await gate.WaitAsync(cancellationToken); try { return Items.Values.Any(x => x.BayId == bayId && x.StartUtc < endUtc && x.EndUtc > startUtc && x.Status is not ShootingRangeBookingStatus.Rejected and not ShootingRangeBookingStatus.Cancelled); } finally { gate.Release(); } } public async Task<ShootingRangeBooking> InsertIfAvailableAsync(ShootingRangeBooking booking, CancellationToken cancellationToken = default) { await gate.WaitAsync(cancellationToken); try { if (Items.Values.Any(x => x.BayId == booking.BayId && x.StartUtc < booking.EndUtc && x.EndUtc > booking.StartUtc && x.Status is not ShootingRangeBookingStatus.Rejected and not ShootingRangeBookingStatus.Cancelled)) throw new InvalidOperationException("The selected slot is already pending or booked."); Items[booking.Id] = booking; return booking; } finally { gate.Release(); } } }
public sealed class MockShootingRangeRepositorySet : IShootingRangeRepositorySet
{
 public IShootingRangeRepository<ShootingRangeConfig> Configs { get; } = new MockShootingRangeRepository<ShootingRangeConfig>();
 public IShootingRangeRepository<ShootingRangeBay> Bays { get; } = new MockShootingRangeRepository<ShootingRangeBay>();
 public IShootingRangeRepository<ShootingRangeException> Exceptions { get; } = new MockShootingRangeRepository<ShootingRangeException>();
 public IShootingRangeUserRepository Users { get; } = new MockShootingRangeUserRepository();
 public IShootingRangeBookingRepository Bookings { get; } = new MockShootingRangeBookingRepository();
 public IShootingRangeRepository<ShootingRangeThread> Threads { get; } = new MockShootingRangeRepository<ShootingRangeThread>();
}
