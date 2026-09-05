using MorWalPizVideo.Models.ShootingRange;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Domain.ShootingRange;

public interface IShootingRangeRepository<T> where T : BaseEntity
{
    Task<T?> GetAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<T> InsertAsync(T item, CancellationToken cancellationToken = default);
    Task<T> ReplaceAsync(T item, CancellationToken cancellationToken = default);
}

public interface IShootingRangeUserRepository : IShootingRangeRepository<ShootingRangeUser>
{
    Task<ShootingRangeUser?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default);
}
public interface IShootingRangeBookingRepository : IShootingRangeRepository<ShootingRangeBooking>
{
    Task<bool> HasOverlapAsync(string bayId, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default);
    Task<ShootingRangeBooking> InsertIfAvailableAsync(ShootingRangeBooking booking, CancellationToken cancellationToken = default);
}
public interface IShootingRangeRepositorySet
{
    IShootingRangeRepository<ShootingRangeConfig> Configs { get; }
    IShootingRangeRepository<ShootingRangeBay> Bays { get; }
    IShootingRangeRepository<ShootingRangeException> Exceptions { get; }
    IShootingRangeUserRepository Users { get; }
    IShootingRangeBookingRepository Bookings { get; }
    IShootingRangeRepository<ShootingRangeThread> Threads { get; }
}
