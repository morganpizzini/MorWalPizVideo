using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MorWalPizVideo.ShootingRange.Models;
using MorWalPizVideo.ShootingRange.Security;

namespace MorWalPizVideo.ShootingRange.Repositories;

public sealed class SqlServerShootingRangeRepositorySet : IShootingRangeRepositorySet
{
    private readonly ShootingRangeDbContext context;
    private readonly AsyncLocal<bool> guarded = new();
    private readonly AsyncLocal<CancellationToken?> operationToken = new();
    private readonly AsyncLocal<IDbContextTransaction?> transaction = new();
    private readonly SemaphoreSlim inMemoryWriteGate;

    public SqlServerShootingRangeRepositorySet(
        ShootingRangeDbContext context,
        TimeProvider? clock = null,
        SemaphoreSlim? inMemoryWriteGate = null)
    {
        this.context = context;
        this.inMemoryWriteGate = inMemoryWriteGate ?? new SemaphoreSlim(1, 1);
        Clock = clock ?? TimeProvider.System;
        TenantContext = context.TenantContext;
        Configs = new SqlServerShootingRangeRepository<ShootingRangeConfig>(this, context);
        Tenants = new SqlServerShootingRangeRepository<ShootingRangeTenant>(this, context);
        Bays = new SqlServerShootingRangeRepository<ShootingRangeBay>(this, context);
        MechanismTypes = new SqlServerShootingRangeRepository<ShootingRangeMechanismType>(this, context);
        Exceptions = new SqlServerShootingRangeRepository<ShootingRangeException>(this, context);
        Users = new SqlServerShootingRangeUserRepository(this, context);
        Bookings = new SqlServerShootingRangeBookingRepository(this, context);
        Threads = new SqlServerShootingRangeRepository<ShootingRangeThread>(this, context);
        LocalSessions = new SqlServerShootingRangeRepository<ShootingRangeLocalSession>(this, context);
        Friendships = new SqlServerShootingRangeFriendshipRepository(this, context);
    }

    internal bool IsGuarded => guarded.Value;
    internal bool HasTransaction => transaction.Value is not null || context.Database.IsInMemory() && guarded.Value;
    internal CancellationToken OperationToken(CancellationToken requested) => operationToken.Value ?? requested;
    public TimeProvider Clock { get; }
    public ShootingRangeTenantContext TenantContext { get; }
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

    public async Task<T> ExecuteGuardedAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        if (guarded.Value) return await action();

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));
        if (context.Database.IsInMemory())
        {
            await inMemoryWriteGate.WaitAsync(deadline.Token);
            guarded.Value = true;
            operationToken.Value = deadline.Token;
            try
            {
                var result = await action();
                await context.SaveChangesAsync(deadline.Token);
                return result;
            }
            finally
            {
                operationToken.Value = null;
                guarded.Value = false;
                context.ChangeTracker.Clear();
                inMemoryWriteGate.Release();
            }
        }

        await using var currentTransaction = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, deadline.Token);
        guarded.Value = true;
        operationToken.Value = deadline.Token;
        transaction.Value = currentTransaction;
        try
        {
            await context.MutationGuards
                .FromSqlRaw("SELECT [id] FROM [shooting_range_mutation_guards] WITH (UPDLOCK, ROWLOCK) WHERE [id] = 1")
                .AsNoTracking()
                .SingleAsync(deadline.Token);
            var result = await action();
            await context.SaveChangesAsync(deadline.Token);
            await currentTransaction.CommitAsync(deadline.Token);
            return result;
        }
        finally
        {
            transaction.Value = null;
            operationToken.Value = null;
            guarded.Value = false;
            context.ChangeTracker.Clear();
        }
    }

    internal async Task VerifyReadyAsync(CancellationToken cancellationToken)
    {
        if (!await context.Database.CanConnectAsync(cancellationToken))
            throw new InvalidOperationException("SQL Server is unavailable.");
        if ((await context.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
            throw new InvalidOperationException("Shooting Range SQL Server schema migration is incomplete.");
        if (!await context.MutationGuards.AsNoTracking().AnyAsync(row => row.Id == 1, cancellationToken))
            throw new InvalidOperationException("Shooting Range SQL Server mutation guard is missing.");
    }
}

public class SqlServerShootingRangeRepository<T>(SqlServerShootingRangeRepositorySet owner, ShootingRangeDbContext context) : IShootingRangeRepository<T>
    where T : BaseEntity
{
    private DbSet<T> Set => context.Set<T>();

    public async Task<T?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var token = owner.OperationToken(cancellationToken);
        var local = Set.Local.FirstOrDefault(item => item.Id == id);
        if (local is not null && IsVisible(local) && context.Entry(local).State is EntityState.Added or EntityState.Modified)
            return local;
        return await Set.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, token) ??
            (local is not null && IsVisible(local) ? local : null);
    }

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var token = owner.OperationToken(cancellationToken);
        var items = (await Set.AsNoTracking().ToListAsync(token)).ToDictionary(item => item.Id);
        foreach (var entry in context.ChangeTracker.Entries<T>())
        {
            if (!IsVisible(entry.Entity))
            {
                items.Remove(entry.Entity.Id);
                continue;
            }
            if (entry.State == EntityState.Deleted) items.Remove(entry.Entity.Id);
            else if (entry.State is EntityState.Added or EntityState.Modified)
                items[entry.Entity.Id] = entry.Entity;
        }
        return items.Values.ToArray();
    }

    public Task<T> InsertAsync(T item, CancellationToken cancellationToken = default) => WriteAsync(item, false, cancellationToken);
    public Task<T> ReplaceAsync(T item, CancellationToken cancellationToken = default) => WriteAsync(item, true, cancellationToken);

    protected Task DeleteRecordAsync(string id, CancellationToken cancellationToken = default) =>
        owner.ExecuteGuardedAsync(async () =>
        {
            if (!owner.HasTransaction)
                throw new InvalidOperationException("A database transaction is required for repository writes.");
            var existing = await GetAsync(id, owner.OperationToken(cancellationToken))
                ?? throw new KeyNotFoundException("Record not found.");
            context.Set<T>().Attach(existing);
            context.Entry(existing).State = EntityState.Deleted;
            return true;
        }, cancellationToken);

    private Task<T> WriteAsync(T item, bool replacing, CancellationToken cancellationToken) =>
        owner.ExecuteGuardedAsync(async () =>
        {
            if (!owner.HasTransaction)
                throw new InvalidOperationException("A database transaction is required for repository writes.");
            item = ShootingRangeWriteValidation.Prepare(item, owner.TenantContext.TenantId);
            await ShootingRangeWriteValidation.ValidateAsync(owner, item, replacing, owner.OperationToken(cancellationToken));

            var token = owner.OperationToken(cancellationToken);
            var existing = await Set.AsNoTracking().AnyAsync(entity => entity.Id == item.Id, token);
            var existsInAnotherTenant = !existing && item is not ShootingRangeTenant &&
                await Set.IgnoreQueryFilters().AsNoTracking().AnyAsync(entity => entity.Id == item.Id, token);
            if (existsInAnotherTenant)
                throw new KeyNotFoundException("Record not found.");
            if (!replacing && existing)
                throw new InvalidOperationException("Duplicate identifier.");
            if (replacing && !existing && typeof(T) == typeof(ShootingRangeBooking))
                throw new KeyNotFoundException("Booking not found.");

            if (Set.Local.FirstOrDefault(entity => entity.Id == item.Id) is { } tracked)
            {
                if (context.Entry(tracked).State == EntityState.Added)
                {
                    if (replacing) throw new KeyNotFoundException("Record not found.");
                    throw new InvalidOperationException("Duplicate identifier.");
                }
                context.Entry(tracked).State = EntityState.Detached;
            }

            if (existing) Set.Update(item);
            else Set.Add(item);
            return item;
        }, cancellationToken);

    private bool IsVisible(T item) => item is ShootingRangeTenant ||
        owner.TenantContext.TenantId is { } tenantId && item.TenantId == tenantId;
}

public sealed class SqlServerShootingRangeFriendshipRepository(
    SqlServerShootingRangeRepositorySet owner,
    ShootingRangeDbContext context)
    : SqlServerShootingRangeRepository<ShootingRangeFriendship>(owner, context), IShootingRangeFriendshipRepository
{
    public Task DeleteFriendshipAsync(string id, CancellationToken cancellationToken = default) =>
        DeleteRecordAsync(id, cancellationToken);
}

public sealed class SqlServerShootingRangeBookingRepository(SqlServerShootingRangeRepositorySet owner, ShootingRangeDbContext context)
    : SqlServerShootingRangeRepository<ShootingRangeBooking>(owner, context), IShootingRangeBookingRepository
{
    private DbSet<ShootingRangeBooking> Set => context.Set<ShootingRangeBooking>();

    public async Task<bool> HasOverlapAsync(string bayId, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default) =>
        (await GetAllAsync(cancellationToken)).Any(item =>
            item.BayId == bayId && item.StartUtc < endUtc && item.EndUtc > startUtc &&
            item.Status is ShootingRangeBookingStatus.Pending or ShootingRangeBookingStatus.Approved);

    public Task<ShootingRangeBooking> InsertIfAvailableAsync(ShootingRangeBooking booking, CancellationToken cancellationToken = default) =>
        InsertAsync(booking, cancellationToken);

    public async Task<(IReadOnlyList<ShootingRangeBooking> Items, int TotalCount)> GetLocalDatePageAsync(
        DateOnly localDate, int offset, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = Set.AsNoTracking().Where(item => item.LocalDate == localDate);
        var totalCount = await query.CountAsync(owner.OperationToken(cancellationToken));
        var items = await query.OrderBy(item => item.StartUtc)
            .ThenBy(item => item.Id)
            .Skip(offset)
            .Take(pageSize)
            .ToListAsync(owner.OperationToken(cancellationToken));
        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<ShootingRangeBooking> Items, int TotalCount)> GetUserReportPageAsync(
        string userId, bool? isNoShow, int offset, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = Set.AsNoTracking().Where(item => item.UserId == userId);
        if (isNoShow.HasValue)
            query = query.Where(item => item.IsNoShow == isNoShow.Value);
        var totalCount = await query.CountAsync(owner.OperationToken(cancellationToken));
        var items = await query.OrderByDescending(item => item.LocalDate)
            .ThenByDescending(item => item.StartUtc)
            .ThenBy(item => item.Id)
            .Skip(offset)
            .Take(pageSize)
            .ToListAsync(owner.OperationToken(cancellationToken));
        return (items, totalCount);
    }

    public async Task<IReadOnlyList<ShootingRangeBooking>> GetNoShowOccurrencesSinceAsync(
        DateOnly earliestDate, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Where(item => item.IsNoShow && item.NoShowDate >= earliestDate)
            .ToListAsync(owner.OperationToken(cancellationToken));
}

public sealed class SqlServerShootingRangeUserRepository(SqlServerShootingRangeRepositorySet owner, ShootingRangeDbContext context)
    : SqlServerShootingRangeRepository<ShootingRangeUser>(owner, context), IShootingRangeUserRepository
{
    public async Task<ShootingRangeUser?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalized = ShootingRangeWriteValidation.NormalizeUsername(username);
        return (await GetAllAsync(cancellationToken)).FirstOrDefault(user =>
            ShootingRangeWriteValidation.NormalizeUsername(user.Username) == normalized);
    }
}
