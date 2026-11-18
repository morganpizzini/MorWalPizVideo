using Microsoft.EntityFrameworkCore;
using MorWalPizVideo.ShootingRange.Migration.LegacyMongo;
using MorWalPizVideo.ShootingRange.Models;
using MorWalPizVideo.ShootingRange.Repositories;
using MongoDB.Driver;

if (args.Length != 1 || args[0] != "--confirm-import")
    throw new InvalidOperationException("Run with --confirm-import only after stopping legacy Range writes and reviewing the source and target.");

var sourceConnectionString = RequiredEnvironmentVariable("SHOOTING_RANGE_IMPORT_SOURCE_MONGO");
var sourceDatabaseName = RequiredEnvironmentVariable("SHOOTING_RANGE_IMPORT_SOURCE_DATABASE");
var targetConnectionString = RequiredEnvironmentVariable("SHOOTING_RANGE_IMPORT_SQLSERVER");

var client = new MongoClient(sourceConnectionString);
var source = client.GetDatabase(sourceDatabaseName);
var configs = await source.GetCollection<LegacyConfig>("shootingRangeConfigs").Find(FilterDefinition<LegacyConfig>.Empty).ToListAsync();
var bays = await source.GetCollection<LegacyBay>("shootingRangeBays").Find(FilterDefinition<LegacyBay>.Empty).ToListAsync();
var exceptions = await source.GetCollection<LegacyException>("shootingRangeExceptions").Find(FilterDefinition<LegacyException>.Empty).ToListAsync();
var users = await source.GetCollection<LegacyUser>("shootingRangeUsers").Find(FilterDefinition<LegacyUser>.Empty).ToListAsync();
var sessions = await source.GetCollection<LegacyLocalSession>("shootingRangeLocalSessions").Find(FilterDefinition<LegacyLocalSession>.Empty).ToListAsync();
var bookings = await source.GetCollection<LegacyBooking>("shootingRangeBookings").Find(FilterDefinition<LegacyBooking>.Empty).ToListAsync();
var threads = await source.GetCollection<LegacyThread>("shootingRangeThreads").Find(FilterDefinition<LegacyThread>.Empty).ToListAsync();

var total = configs.Count + bays.Count + exceptions.Count + users.Count + sessions.Count + bookings.Count + threads.Count;
if (total == 0)
    throw new InvalidOperationException("No legacy Range records were found. Verify the source database before choosing the empty-database bootstrap path.");

ValidateUsers(users);

var options = new DbContextOptionsBuilder<ShootingRangeDbContext>()
    .UseSqlServer(targetConnectionString)
    .Options;
await using var database = new ShootingRangeDbContext(options);
var pendingMigrations = await database.Database.GetPendingMigrationsAsync();
if (pendingMigrations.Any())
    throw new InvalidOperationException("The Azure SQL target has pending schema migrations. Apply them explicitly before importing.");

await using var transaction = await database.Database.BeginTransactionAsync();
var guard = await database.MutationGuards
    .FromSqlRaw("SELECT [id] FROM [shooting_range_mutation_guards] WITH (UPDLOCK, ROWLOCK) WHERE [id] = 1")
    .SingleOrDefaultAsync();
if (guard is null)
    throw new InvalidOperationException("The Azure SQL mutation guard is missing. Apply the schema migration before importing.");

if (await database.Configs.AnyAsync() ||
    await database.Bays.AnyAsync() ||
    await database.Exceptions.AnyAsync() ||
    await database.Users.AnyAsync() ||
    await database.LocalSessions.AnyAsync() ||
    await database.Bookings.AnyAsync() ||
    await database.Threads.AnyAsync())
    throw new InvalidOperationException("The Azure SQL target contains Range records. Import is allowed only into an empty target.");

database.Configs.AddRange(configs.Select(item => new ShootingRangeConfig
{
    Id = item.Id,
    CreationDateTime = item.CreationDateTime,
    Name = item.Name,
    TimeZone = item.TimeZone,
    SessionMode = item.SessionMode,
    OpeningDays = item.OpeningDays,
    MorningStart = item.MorningStart,
    MorningEnd = item.MorningEnd,
    AfternoonStart = item.AfternoonStart,
    AfternoonEnd = item.AfternoonEnd,
    HourlyMinutes = item.HourlyMinutes,
    ContinuousStart = item.ContinuousStart,
    ContinuousEnd = item.ContinuousEnd,
    ReservedReleaseDaysBefore = item.ReservedReleaseDaysBefore
}));
database.Bays.AddRange(bays.Select(item => new ShootingRangeBay
{
    Id = item.Id,
    CreationDateTime = item.CreationDateTime,
    Code = item.Code,
    Mechanisms = item.Mechanisms.Select(mechanism => new Mechanism(mechanism.Type, mechanism.Quantity)).ToList(),
    Partitions = item.Partitions,
    Plates = item.Plates,
    Pepper = item.Pepper,
    Status = item.Status,
    Description = item.Description,
    ReservedUntilUtc = item.ReservedUntilUtc,
    WhitelistUserIds = item.WhitelistUserIds
}));
database.Exceptions.AddRange(exceptions.Select(item => new ShootingRangeException
{
    Id = item.Id,
    CreationDateTime = item.CreationDateTime,
    LocalDate = item.LocalDate,
    BayId = item.BayId,
    IsClosed = item.IsClosed,
    Reason = item.Reason
}));
database.Users.AddRange(users.Select(item => new ShootingRangeUser
{
    Id = item.Id,
    CreationDateTime = item.CreationDateTime,
    Username = item.Username,
    NormalizedUsername = item.NormalizedUsername,
    FirstName = item.FirstName,
    LastName = item.LastName,
    PasswordHash = item.PasswordHash,
    Status = item.Status,
    IsAdmin = item.IsAdmin,
    ForcePasswordChange = item.ForcePasswordChange,
    SecurityVersion = item.SecurityVersion
}));
database.LocalSessions.AddRange(sessions.Select(item => new ShootingRangeLocalSession
{
    Id = item.Id,
    CreationDateTime = item.CreationDateTime,
    UserId = item.UserId,
    SecurityVersion = item.SecurityVersion,
    ExpiresUtc = item.ExpiresUtc,
    Revoked = item.Revoked
}));
database.Bookings.AddRange(bookings.Select(item => new ShootingRangeBooking
{
    Id = item.Id,
    CreationDateTime = item.CreationDateTime,
    UserId = item.UserId,
    BayId = item.BayId,
    LocalDate = item.LocalDate,
    PeriodKey = item.PeriodKey,
    StartUtc = item.StartUtc,
    EndUtc = item.EndUtc,
    Request = item.Request,
    Status = item.Status
}));
database.Threads.AddRange(threads.Select(item => new ShootingRangeThread
{
    Id = item.Id,
    CreationDateTime = item.CreationDateTime,
    UserId = item.UserId,
    Subject = item.Subject,
    Messages = item.Messages.Select(message =>
        new ShootingRangeMessage(message.AuthorUserId, message.FromAdmin, message.Text, message.CreatedUtc)).ToList()
}));

await database.SaveChangesAsync();
if (await database.Configs.CountAsync() != configs.Count ||
    await database.Bays.CountAsync() != bays.Count ||
    await database.Exceptions.CountAsync() != exceptions.Count ||
    await database.Users.CountAsync() != users.Count ||
    await database.LocalSessions.CountAsync() != sessions.Count ||
    await database.Bookings.CountAsync() != bookings.Count ||
    await database.Threads.CountAsync() != threads.Count)
    throw new InvalidOperationException("Azure SQL import verification failed; the transaction will be rolled back.");

await transaction.CommitAsync();

Console.WriteLine($"Imported Range records: configs={configs.Count}, bays={bays.Count}, exceptions={exceptions.Count}, users={users.Count}, sessions={sessions.Count}, bookings={bookings.Count}, threads={threads.Count}.");

static string RequiredEnvironmentVariable(string name) =>
    Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
        ? value
        : throw new InvalidOperationException($"Required environment variable {name} is missing.");

static void ValidateUsers(IReadOnlyCollection<LegacyUser> users)
{
    var duplicates = users.GroupBy(user => user.Username.Trim().ToLowerInvariant(), StringComparer.Ordinal)
        .Any(group => group.Count() > 1);
    if (users.Any(user => string.IsNullOrWhiteSpace(user.Username) ||
                          user.NormalizedUsername != user.Username.Trim().ToLowerInvariant()) || duplicates)
        throw new InvalidOperationException("Legacy usernames are empty, duplicated, or have inconsistent normalized values. Resolve explicitly before import.");
}
