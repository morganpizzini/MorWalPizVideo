using MongoDB.Bson;
using MongoDB.Driver;
using MorWalPizVideo.ShootingRange.Models;

namespace MorWalPizVideo.ShootingRange.Repositories;

public class MongoShootingRangeRepository<T>(MongoShootingRangeRepositorySet owner, IMongoDatabase database, string collectionName) : IShootingRangeRepository<T> where T : BaseEntity
{
    protected readonly IMongoCollection<T> Collection = database.GetCollection<T>(collectionName);
    public async Task<T?> GetAsync(string id, CancellationToken cancellationToken = default) => owner.Session is { } session
        ? await Collection.Find(session, Builders<T>.Filter.Eq(item => item.Id, id)).FirstOrDefaultAsync(owner.OperationToken(cancellationToken))
        : await Collection.Find(item => item.Id == id).FirstOrDefaultAsync(cancellationToken);
    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default) => owner.Session is { } session
        ? await Collection.Find(session, Builders<T>.Filter.Empty).ToListAsync(owner.OperationToken(cancellationToken))
        : await Collection.Find(Builders<T>.Filter.Empty).ToListAsync(cancellationToken);
    public Task<T> InsertAsync(T item, CancellationToken cancellationToken = default) => WriteAsync(item, false, cancellationToken);
    public Task<T> ReplaceAsync(T item, CancellationToken cancellationToken = default) => WriteAsync(item, true, cancellationToken);
    private Task<T> WriteAsync(T item, bool replacing, CancellationToken cancellationToken) => owner.ExecuteGuardedAsync(async () =>
    {
        item = ShootingRangeWriteValidation.Prepare(item);
        await ShootingRangeWriteValidation.ValidateAsync(owner, item, replacing, cancellationToken);
        if (replacing)
            await Collection.ReplaceOneAsync(owner.Session!, Builders<T>.Filter.Eq(entity => entity.Id, item.Id), item, new ReplaceOptions { IsUpsert = typeof(T) != typeof(ShootingRangeBooking) }, owner.OperationToken(cancellationToken));
        else await Collection.InsertOneAsync(owner.Session!, item, cancellationToken: owner.OperationToken(cancellationToken));
        return item;
    }, cancellationToken);
}

public sealed class MongoShootingRangeBookingRepository : MongoShootingRangeRepository<ShootingRangeBooking>, IShootingRangeBookingRepository
{
    public MongoShootingRangeBookingRepository(MongoShootingRangeRepositorySet owner, IMongoDatabase database) : base(owner, database, "shootingRangeBookings")
    {
        Collection.Indexes.CreateOne(new CreateIndexModel<ShootingRangeBooking>(Builders<ShootingRangeBooking>.IndexKeys.Ascending(x => x.BayId).Ascending(x => x.LocalDate).Ascending(x => x.PeriodKey), new CreateIndexOptions<ShootingRangeBooking> { Unique = true, PartialFilterExpression = new BsonDocument("status", new BsonDocument("$in", new BsonArray { new BsonInt32(0), new BsonInt32(1) })) }));
    }

    public async Task<bool> HasOverlapAsync(string bayId, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default) => (await GetAllAsync(cancellationToken)).Any(item => item.BayId == bayId && item.StartUtc < endUtc && item.EndUtc > startUtc && item.Status is ShootingRangeBookingStatus.Pending or ShootingRangeBookingStatus.Approved);
    public Task<ShootingRangeBooking> InsertIfAvailableAsync(ShootingRangeBooking booking, CancellationToken cancellationToken = default) => InsertAsync(booking, cancellationToken);
}

public sealed class MongoShootingRangeRepositorySet : IShootingRangeRepositorySet
{
    private readonly IMongoDatabase database;
    private readonly AsyncLocal<IClientSessionHandle?> currentSession = new();
    private readonly AsyncLocal<CancellationToken?> transactionToken = new();
    internal IClientSessionHandle? Session => currentSession.Value;
    internal CancellationToken OperationToken(CancellationToken requested) => transactionToken.Value ?? requested;
    public TimeProvider Clock { get; }
    public MongoShootingRangeRepositorySet(IMongoDatabase database, TimeProvider? clock = null)
    {
        this.database = database;
        Clock = clock ?? TimeProvider.System;
        Configs = new MongoShootingRangeRepository<ShootingRangeConfig>(this, database, "shootingRangeConfigs");
        Bays = new MongoShootingRangeRepository<ShootingRangeBay>(this, database, "shootingRangeBays");
        Exceptions = new MongoShootingRangeRepository<ShootingRangeException>(this, database, "shootingRangeExceptions");
        Users = new MongoShootingRangeUserRepository(this, database);
        Bookings = new MongoShootingRangeBookingRepository(this, database);
        Threads = new MongoShootingRangeRepository<ShootingRangeThread>(this, database, "shootingRangeThreads");
        LocalSessions = new MongoShootingRangeRepository<ShootingRangeLocalSession>(this, database, "shootingRangeLocalSessions");
    }
    public async Task<T> ExecuteGuardedAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        if (Session is not null) return await action();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));
        using var session = await database.Client.StartSessionAsync(cancellationToken: deadline.Token);
        await database.GetCollection<BsonDocument>("shootingRangeMutationGuard").UpdateOneAsync(
            new BsonDocument("_id", "field"), new BsonDocument("$setOnInsert", new BsonDocument("revision", 0L)),
            new UpdateOptions { IsUpsert = true }, deadline.Token);
        return await session.WithTransactionAsync(async (transactionSession, retryToken) =>
        {
            currentSession.Value = transactionSession;
            transactionToken.Value = retryToken;
            try
            {
                await database.GetCollection<BsonDocument>("shootingRangeMutationGuard").UpdateOneAsync(transactionSession,
                    new BsonDocument("_id", "field"), new BsonDocument("$inc", new BsonDocument("revision", 1L)),
                    new UpdateOptions { IsUpsert = true }, retryToken);
                return await action();
            }
            finally { currentSession.Value = null; transactionToken.Value = null; }
        }, new TransactionOptions(readConcern: ReadConcern.Snapshot, writeConcern: WriteConcern.WMajority, readPreference: ReadPreference.Primary), deadline.Token);
    }
    public IShootingRangeRepository<ShootingRangeConfig> Configs { get; }
    public async Task VerifySecurityIndexesAsync(CancellationToken cancellationToken = default)
    {
        ShootingRangeWriteValidation.ValidateUsernamePreflight(await Users.GetAllAsync(cancellationToken));
        await database.GetCollection<ShootingRangeUser>("shootingRangeUsers").Indexes.CreateOneAsync(
            new CreateIndexModel<ShootingRangeUser>(Builders<ShootingRangeUser>.IndexKeys.Ascending(user => user.NormalizedUsername), new CreateIndexOptions { Unique = true, Name = "normalized_username_unique" }), cancellationToken: cancellationToken);
        await database.GetCollection<ShootingRangeLocalSession>("shootingRangeLocalSessions").Indexes.CreateOneAsync(
            new CreateIndexModel<ShootingRangeLocalSession>(Builders<ShootingRangeLocalSession>.IndexKeys.Ascending(session => session.ExpiresUtc), new CreateIndexOptions { ExpireAfter = TimeSpan.Zero, Name = "local_session_expiry" }), cancellationToken: cancellationToken);
    }

    public async Task VerifyTransactionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await ExecuteGuardedAsync<bool>(async () =>
            {
                await database.GetCollection<BsonDocument>("shootingRangeConfigs").InsertOneAsync(Session!,
                    new BsonDocument("_id", ObjectId.GenerateNewId()), cancellationToken: OperationToken(cancellationToken));
                throw new TransactionProbeRollbackException();
            }, cancellationToken);
        }
        catch (TransactionProbeRollbackException) { }
    }

    private sealed class TransactionProbeRollbackException : Exception;

    public IShootingRangeRepository<ShootingRangeBay> Bays { get; }
    public IShootingRangeRepository<ShootingRangeException> Exceptions { get; }
    public IShootingRangeUserRepository Users { get; }
    public IShootingRangeBookingRepository Bookings { get; }
    public IShootingRangeRepository<ShootingRangeThread> Threads { get; }
    public IShootingRangeRepository<ShootingRangeLocalSession> LocalSessions { get; }
}

public sealed class MongoShootingRangeUserRepository(MongoShootingRangeRepositorySet owner, IMongoDatabase database) : MongoShootingRangeRepository<ShootingRangeUser>(owner, database, "shootingRangeUsers"), IShootingRangeUserRepository
{
    public async Task<ShootingRangeUser?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default) => (await GetAllAsync(cancellationToken)).FirstOrDefault(item => ShootingRangeWriteValidation.NormalizeUsername(item.Username) == ShootingRangeWriteValidation.NormalizeUsername(username));
}