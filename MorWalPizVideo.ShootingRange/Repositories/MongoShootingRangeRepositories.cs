using MongoDB.Bson;
using MongoDB.Driver;
using MorWalPizVideo.ShootingRange.Models;

namespace MorWalPizVideo.ShootingRange.Repositories;

public class MongoShootingRangeRepository<T>(IMongoDatabase database, string collectionName) : IShootingRangeRepository<T> where T : BaseEntity
{
    private readonly IMongoCollection<T> collection = database.GetCollection<T>(collectionName);
    public async Task<T?> GetAsync(string id, CancellationToken cancellationToken = default) => await collection.Find(Builders<T>.Filter.Eq("_id", id)).FirstOrDefaultAsync(cancellationToken);
    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default) => await collection.Find(Builders<T>.Filter.Empty).ToListAsync(cancellationToken);
    public async Task<T> InsertAsync(T item, CancellationToken cancellationToken = default) { await collection.InsertOneAsync(item, cancellationToken: cancellationToken); return item; }
    public async Task<T> ReplaceAsync(T item, CancellationToken cancellationToken = default) { await collection.ReplaceOneAsync(Builders<T>.Filter.Eq("_id", item.Id), item, cancellationToken: cancellationToken); return item; }
}

public sealed class MongoShootingRangeBookingRepository : MongoShootingRangeRepository<ShootingRangeBooking>, IShootingRangeBookingRepository
{
    private readonly IMongoCollection<ShootingRangeBooking> collection;

    public MongoShootingRangeBookingRepository(IMongoDatabase database) : base(database, "shootingRangeBookings")
    {
        collection = database.GetCollection<ShootingRangeBooking>("shootingRangeBookings");
        collection.Indexes.CreateOne(new CreateIndexModel<ShootingRangeBooking>(Builders<ShootingRangeBooking>.IndexKeys.Ascending(x => x.BayId).Ascending(x => x.LocalDate).Ascending(x => x.PeriodKey), new CreateIndexOptions<ShootingRangeBooking> { Unique = true, PartialFilterExpression = new BsonDocument("status", new BsonDocument("$in", new BsonArray { new BsonInt32(0), new BsonInt32(1) })) }));
    }

    public Task<bool> HasOverlapAsync(string bayId, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default) => collection.Find(x => x.BayId == bayId && x.StartUtc < endUtc && x.EndUtc > startUtc && x.Status != ShootingRangeBookingStatus.Rejected && x.Status != ShootingRangeBookingStatus.Cancelled).AnyAsync(cancellationToken);
    public async Task<ShootingRangeBooking> InsertIfAvailableAsync(ShootingRangeBooking booking, CancellationToken cancellationToken = default) { try { await collection.InsertOneAsync(booking, cancellationToken: cancellationToken); return booking; } catch (MongoWriteException error) when (error.WriteError?.Category == ServerErrorCategory.DuplicateKey) { throw new InvalidOperationException("The selected slot is already pending or booked.", error); } }
}

public sealed class MongoShootingRangeRepositorySet(IMongoDatabase database) : IShootingRangeRepositorySet
{
    public IShootingRangeRepository<ShootingRangeConfig> Configs { get; } = new MongoShootingRangeRepository<ShootingRangeConfig>(database, "shootingRangeConfigs");
    public IShootingRangeRepository<ShootingRangeBay> Bays { get; } = new MongoShootingRangeRepository<ShootingRangeBay>(database, "shootingRangeBays");
    public IShootingRangeRepository<ShootingRangeException> Exceptions { get; } = new MongoShootingRangeRepository<ShootingRangeException>(database, "shootingRangeExceptions");
    public IShootingRangeUserRepository Users { get; } = new MongoShootingRangeUserRepository(database);
    public IShootingRangeBookingRepository Bookings { get; } = new MongoShootingRangeBookingRepository(database);
    public IShootingRangeRepository<ShootingRangeThread> Threads { get; } = new MongoShootingRangeRepository<ShootingRangeThread>(database, "shootingRangeThreads");
}

public sealed class MongoShootingRangeUserRepository(IMongoDatabase database) : MongoShootingRangeRepository<ShootingRangeUser>(database, "shootingRangeUsers"), IShootingRangeUserRepository
{
    private readonly IMongoCollection<ShootingRangeUser> collection = database.GetCollection<ShootingRangeUser>("shootingRangeUsers");
    public async Task<ShootingRangeUser?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default) => await collection.Find(x => x.Username == username).FirstOrDefaultAsync(cancellationToken);
}