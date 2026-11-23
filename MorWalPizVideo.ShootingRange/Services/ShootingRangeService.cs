using System.Security.Claims;
using System.Security.Cryptography;
using MorWalPizVideo.ShootingRange.Contracts;
using MorWalPizVideo.ShootingRange.Models;
using MorWalPizVideo.ShootingRange.Repositories;
using MorWalPizVideo.ShootingRange.Security;

namespace MorWalPizVideo.ShootingRange.Services;

public sealed class ShootingRangeService(IShootingRangeRepositorySet repositories)
{
    public async Task<ShootingRangeUser?> AuthenticateAsync(string username, string password)
    {
        var user = await repositories.Users.FindByUsernameAsync(username.Trim().ToLowerInvariant());
        if (user is null || user.Status != ShootingRangeAccountStatus.Approved) return null;
        var parts = user.PasswordHash.Split(':', 2);
        return parts.Length == 2 && PasswordHashing.VerifyPassword(password, parts[0], parts[1]) ? user : null;
    }

    public async Task<ShootingRangeConfig> GetConfigAsync(CancellationToken cancellationToken) => await LoadConfigAsync(repositories, cancellationToken);

    private static async Task<ShootingRangeConfig> LoadConfigAsync(IShootingRangeRepositorySet repositories, CancellationToken cancellationToken) =>
        (await repositories.Configs.GetAllAsync(cancellationToken)).FirstOrDefault()
        ?? new ShootingRangeConfig
        {
            Id = ShootingRangeIds.NewId(),
            TenantId = repositories.TenantContext.TenantId ?? string.Empty
        };

    public async Task<SessionsDto> GetSessionsAsync(DateOnly localDate, CancellationToken cancellationToken)
    {
        var config = await GetConfigAsync(cancellationToken);
        var sessions = GenerateSessions(config, localDate);
        var closures = await repositories.Exceptions.GetAllAsync(cancellationToken);
        return new(localDate, config.TimeZone, !config.OpeningDays.Contains(localDate.DayOfWeek) || closures.Any(closure => closure.LocalDate == localDate && closure.IsClosed && closure.BayId is null)
            ? [] : sessions.Where(session => session.StartUtc > repositories.Clock.GetUtcNow().UtcDateTime).ToArray());
    }

    public async Task<AvailabilityDto> GetAvailabilityAsync(DateOnly localDate, string periodKey, CancellationToken cancellationToken, string? userId = null)
    {
        var config = await GetConfigAsync(cancellationToken);
        var zone = TimeZoneInfo.FindSystemTimeZoneById(config.TimeZone);
        var (start, end) = ResolvePeriod(config, localDate, periodKey);
        var exceptions = await repositories.Exceptions.GetAllAsync(cancellationToken);
        if (start <= repositories.Clock.GetUtcNow().UtcDateTime || !config.OpeningDays.Contains(localDate.DayOfWeek) || exceptions.Any(x => x.LocalDate == localDate && x.IsClosed && x.BayId is null)) return new(localDate, periodKey, start, end, [], []);
        var bays = await repositories.Bays.GetAllAsync(cancellationToken);
        var dayExceptionBayIds = exceptions.Where(x => x.LocalDate == localDate && x.IsClosed && x.BayId is not null).Select(x => x.BayId!).ToHashSet();
        var nowLocal = TimeZoneInfo.ConvertTime(repositories.Clock.GetUtcNow(), zone).Date;
        var daysUntil = localDate.DayNumber - DateOnly.FromDateTime(nowLocal).DayNumber;
        var bookings = await repositories.Bookings.GetAllAsync(cancellationToken);
        var users = await repositories.Users.GetAllAsync(cancellationToken);
        var requestingUser = userId is null ? null : await repositories.Users.GetAsync(userId, cancellationToken);
        var friends = requestingUser is not { Status: ShootingRangeAccountStatus.Approved, ForcePasswordChange: false }
            ? [] : await GetFriendIdsAsync(userId!, cancellationToken);
        bool CanJoin(ShootingRangeBay bay)
        {
            var occupants = bookings.Where(booking => BlocksBay(booking, bay.Id, start, end)).ToArray();
            return occupants.Length > 0 &&
                occupants.All(booking => booking.StartUtc == start && booking.EndUtc == end &&
                    booking.StartUtc.Kind == DateTimeKind.Utc && booking.EndUtc.Kind == DateTimeKind.Utc &&
                    booking.StartUtc < booking.EndUtc) &&
                occupants.Select(booking => booking.UserId).Distinct(StringComparer.Ordinal).Count() == occupants.Length &&
                userId is not null && occupants.All(booking => booking.UserId != userId) &&
                !bookings.Any(booking => booking.UserId == userId && booking.BayId == bay.Id &&
                    booking.StartUtc == start && booking.EndUtc == end &&
                    booking.Status is ShootingRangeBookingStatus.Pending or ShootingRangeBookingStatus.Approved) &&
                occupants.Any(booking => friends.Contains(booking.UserId));
        }
        var visible = bays.Where(b => b.Status == ShootingRangeBayStatus.Available && !dayExceptionBayIds.Contains(b.Id))
            .Where(b => !bookings.Any(booking => BlocksBay(booking, b.Id, start, end)) || CanJoin(b))
            .Where(b => daysUntil <= config.ReservedReleaseDaysBefore || b.WhitelistUserIds.Count == 0 || userId is not null && b.WhitelistUserIds.Contains(userId)).ToArray();
        var visibleBayIds = visible.Select(bay => bay.Id).ToHashSet(StringComparer.Ordinal);
        var friendBookings = bookings
            .Where(booking => visibleBayIds.Contains(booking.BayId) && booking.StartUtc == start && booking.EndUtc == end &&
                (booking.Status is ShootingRangeBookingStatus.Pending or ShootingRangeBookingStatus.Approved) &&
                friends.Contains(booking.UserId))
            .Select(booking => (Booking: booking, User: users.FirstOrDefault(user => user.Id == booking.UserId)))
            .Where(item => item.User is not null)
            .Select(item => new FriendBookingDto(item.Booking.BayId, item.User!.Username, item.Booking.Status))
            .ToArray();
        return new(localDate, periodKey, start, end, visible, friendBookings);
    }

    public Task<T> AdminMutationAsync<T>(string actorId, Func<Task<T>> mutation, CancellationToken cancellationToken) => repositories.ExecuteGuardedAsync(async () =>
    {
        await RequireAdminAsync(actorId, cancellationToken);
        return await mutation();
    }, cancellationToken);

    public async Task RequireAdminAsync(string actorId, CancellationToken cancellationToken)
    {
        var actor = await repositories.Users.GetAsync(actorId, cancellationToken);
        if (actor is null || !actor.IsAdmin || actor.Status != ShootingRangeAccountStatus.Approved || actor.ForcePasswordChange)
            throw new UnauthorizedAccessException("A currently approved administrator is required.");
    }

    public Task<ShootingRangeMechanismType> CreateMechanismTypeAsync(string actorId, string name, CancellationToken cancellationToken) =>
        AdminMutationAsync(actorId, async () =>
        {
            var normalizedName = name.Trim();
            if (string.IsNullOrWhiteSpace(normalizedName) || normalizedName.Length > 80)
                throw new ArgumentException("Mechanism type name must contain 1 to 80 characters.");
            return await repositories.MechanismTypes.InsertAsync(new ShootingRangeMechanismType
            {
                Id = ShootingRangeIds.NewId(),
                Name = normalizedName
            }, cancellationToken);
        }, cancellationToken);

    public Task<ShootingRangeBooking> DecideBookingAsync(string actorId, string id, bool approved, CancellationToken cancellationToken) => AdminMutationAsync(actorId, async () =>
    {
        var booking = await repositories.Bookings.GetAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Booking not found.");
        if (booking.Status == ShootingRangeBookingStatus.Cancelled && !approved)
            throw new InvalidOperationException("A cancelled booking can only be restored by approval.");
        if (approved && booking.StartUtc <= repositories.Clock.GetUtcNow().UtcDateTime)
            throw new InvalidOperationException("A booking cannot be approved after its session has started.");
        return await repositories.Bookings.ReplaceAsync(booking with { Status = approved ? ShootingRangeBookingStatus.Approved : ShootingRangeBookingStatus.Rejected }, cancellationToken);
    }, cancellationToken);

    public Task<ShootingRangeBooking> CancelBookingAsync(string userId, string id, CancellationToken cancellationToken) =>
        repositories.ExecuteGuardedAsync(async () =>
        {
            var user = await repositories.Users.GetAsync(userId, cancellationToken);
            if (user is null || user.Status != ShootingRangeAccountStatus.Approved || user.ForcePasswordChange)
                throw new UnauthorizedAccessException("An approved account without a required password change is necessary.");
            var booking = await repositories.Bookings.GetAsync(id, cancellationToken);
            if (booking is null || booking.UserId != userId)
                throw new KeyNotFoundException("Booking not found.");
            if (booking.Status is not ShootingRangeBookingStatus.Pending and not ShootingRangeBookingStatus.Approved ||
                booking.StartUtc <= repositories.Clock.GetUtcNow().UtcDateTime)
                throw new InvalidOperationException("Only a future pending or approved booking can be cancelled.");
            return await repositories.Bookings.ReplaceAsync(booking with { Status = ShootingRangeBookingStatus.Cancelled }, cancellationToken);
        }, cancellationToken);

    public Task<ShootingRangeThread> DecideThreadStatusAsync(
        string actorId,
        string threadId,
        ShootingRangeThreadStatus status,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(status) || status == ShootingRangeThreadStatus.Pending)
            throw new ArgumentException("Choose a valid message status.", nameof(status));

        return AdminMutationAsync(actorId, async () =>
        {
            var thread = await repositories.Threads.GetAsync(threadId, cancellationToken)
                ?? throw new KeyNotFoundException("Message not found.");
            return await repositories.Threads.ReplaceAsync(thread with { Status = status }, cancellationToken);
        }, cancellationToken);
    }

    public Task<ShootingRangeThread> ReplyToThreadAsync(
        string actorId,
        string threadId,
        string text,
        CancellationToken cancellationToken) =>
        repositories.ExecuteGuardedAsync(async () =>
        {
            var actor = await repositories.Users.GetAsync(actorId, cancellationToken);
            if (actor is null || actor.Status != ShootingRangeAccountStatus.Approved || actor.ForcePasswordChange)
                throw new UnauthorizedAccessException("An approved account without a required password change is necessary.");

            var thread = await repositories.Threads.GetAsync(threadId, cancellationToken);
            if (thread is null || !actor.IsAdmin && thread.UserId != actorId)
                throw new KeyNotFoundException("Message not found.");

            var updated = thread with
            {
                Messages = [.. (thread.Messages ?? []), new(actorId, actor.IsAdmin, text.Trim(), DateTime.UtcNow)]
            };
            return await repositories.Threads.ReplaceAsync(updated, cancellationToken);
        }, cancellationToken);

    public async Task<IReadOnlyList<ShootingRangeBooking>> GetUserBookingsAsync(string userId, CancellationToken cancellationToken)
    {
        var firstVisibleDate = await GetFirstVisibleBookingDateAsync(cancellationToken);
        return (await repositories.Bookings.GetAllAsync(cancellationToken))
            .Where(booking => booking.UserId == userId && booking.LocalDate >= firstVisibleDate)
            .OrderByDescending(booking => booking.StartUtc)
            .ToArray();
    }

    public async Task<IReadOnlyList<ShootingRangeBooking>> GetAdminBookingsAsync(string actorId, CancellationToken cancellationToken)
    {
        await RequireAdminAsync(actorId, cancellationToken);
        var firstVisibleDate = await GetFirstVisibleBookingDateAsync(cancellationToken);
        return (await repositories.Bookings.GetAllAsync(cancellationToken))
            .Where(booking => booking.LocalDate >= firstVisibleDate)
            .OrderByDescending(booking => booking.StartUtc)
            .ToArray();
    }

    public async Task<int> GetPendingBookingCountAsync(string actorId, CancellationToken cancellationToken)
    {
        await RequireAdminAsync(actorId, cancellationToken);
        var now = repositories.Clock.GetUtcNow().UtcDateTime;
        return (await repositories.Bookings.GetAllAsync(cancellationToken))
            .Count(booking => booking.Status == ShootingRangeBookingStatus.Pending && booking.StartUtc > now);
    }

    public Task<IReadOnlyList<FriendDto>> GetFriendsAsync(string userId, CancellationToken cancellationToken) =>
        repositories.ExecuteGuardedAsync(async () =>
        {
            var actor = await RequireApprovedMemberAsync(userId, cancellationToken);
            var allUsers = await repositories.Users.GetAllAsync(cancellationToken);
            var friends = await GetFriendIdsAsync(actor.Id, cancellationToken);
            return (IReadOnlyList<FriendDto>)allUsers.Where(user => friends.Contains(user.Id) && user.Status == ShootingRangeAccountStatus.Approved)
                .OrderBy(user => user.Username, StringComparer.OrdinalIgnoreCase)
                .Select(user => new FriendDto(user.Id, user.Username, user.FirstName, user.LastName))
                .ToArray();
        }, cancellationToken);

    public Task RequestFriendAsync(string userId, string username, CancellationToken cancellationToken) =>
        repositories.ExecuteGuardedAsync(async () =>
        {
            var actor = await RequireApprovedMemberAsync(userId, cancellationToken);
            if (string.IsNullOrWhiteSpace(username))
                throw new InvalidOperationException("Unable to create friend request.");
            var target = await repositories.Users.FindByUsernameAsync(username.Trim(), cancellationToken);
            if (target is null || target.Id == actor.Id)
                throw new InvalidOperationException("Unable to create friend request.");
            var (userAId, userBId) = OrderPair(actor.Id, target.Id);
            var existing = (await repositories.Friendships.GetAllAsync(cancellationToken))
                .FirstOrDefault(friendship => friendship.UserAId == userAId && friendship.UserBId == userBId);
            if (existing is { Status: ShootingRangeFriendshipStatus.Pending, RequesterUserId: var requesterId })
            {
                if (requesterId == actor.Id) return true;
                throw new InvalidOperationException("Unable to create friend request.");
            }
            if (existing is { Status: ShootingRangeFriendshipStatus.Accepted })
                throw new InvalidOperationException("Unable to create friend request.");
            if (existing is not null)
                throw new InvalidOperationException("Unable to create friend request.");
            if (target is not { Status: ShootingRangeAccountStatus.Approved, ForcePasswordChange: false })
                throw new InvalidOperationException("Unable to create friend request.");
            await repositories.Friendships.InsertAsync(new ShootingRangeFriendship
            {
                Id = ShootingRangeIds.NewId(),
                UserAId = userAId,
                UserBId = userBId,
                Status = ShootingRangeFriendshipStatus.Pending,
                RequesterUserId = actor.Id,
                CreationDateTime = repositories.Clock.GetUtcNow().UtcDateTime
            }, cancellationToken);
            return true;
        }, cancellationToken);

    public Task<FriendRequestsDto> GetFriendRequestsAsync(string userId, CancellationToken cancellationToken) =>
        repositories.ExecuteGuardedAsync(async () =>
        {
            var actor = await RequireApprovedMemberAsync(userId, cancellationToken);
            var usersById = (await repositories.Users.GetAllAsync(cancellationToken))
                .Where(user => user.Status == ShootingRangeAccountStatus.Approved)
                .ToDictionary(user => user.Id, StringComparer.Ordinal);
            var incoming = new List<FriendRequestDto>();
            var outgoing = new List<FriendRequestDto>();
            foreach (var friendship in await repositories.Friendships.GetAllAsync(cancellationToken))
            {
                if (friendship.Status != ShootingRangeFriendshipStatus.Pending ||
                    friendship.RequesterUserId is not { } requesterId ||
                    requesterId != friendship.UserAId && requesterId != friendship.UserBId ||
                    friendship.UserAId != actor.Id && friendship.UserBId != actor.Id)
                    continue;

                var otherUserId = friendship.UserAId == actor.Id ? friendship.UserBId : friendship.UserAId;
                if (!usersById.TryGetValue(otherUserId, out var otherUser)) continue;
                var request = new FriendRequestDto(
                    friendship.Id,
                    otherUser.Username,
                    otherUser.FirstName,
                    otherUser.LastName,
                    DateTime.SpecifyKind(friendship.CreationDateTime, DateTimeKind.Utc));
                (requesterId == actor.Id ? outgoing : incoming).Add(request);
            }
            return new FriendRequestsDto(
                incoming.OrderBy(item => item.Username, StringComparer.OrdinalIgnoreCase).ToArray(),
                outgoing.OrderBy(item => item.Username, StringComparer.OrdinalIgnoreCase).ToArray());
        }, cancellationToken);

    public Task RespondToFriendRequestAsync(string userId, string requestId, bool accept, CancellationToken cancellationToken) =>
        repositories.ExecuteGuardedAsync(async () =>
        {
            var actor = await RequireApprovedMemberAsync(userId, cancellationToken);
            var friendship = await repositories.Friendships.GetAsync(requestId, cancellationToken);
            if (friendship is not { Status: ShootingRangeFriendshipStatus.Pending } ||
                friendship.RequesterUserId is not { } requesterId || requesterId == actor.Id ||
                friendship.UserAId != actor.Id && friendship.UserBId != actor.Id)
                throw new InvalidOperationException("Unable to decide friend request.");

            if (accept)
            {
                var requester = await repositories.Users.GetAsync(requesterId, cancellationToken);
                if (requester is not { Status: ShootingRangeAccountStatus.Approved, ForcePasswordChange: false })
                    throw new InvalidOperationException("Unable to decide friend request.");
                await repositories.Friendships.ReplaceAsync(friendship with { Status = ShootingRangeFriendshipStatus.Accepted }, cancellationToken);
            }
            else
                await repositories.Friendships.DeleteFriendshipAsync(friendship.Id, cancellationToken);
            return true;
        }, cancellationToken);

    public Task CancelFriendRequestAsync(string userId, string requestId, CancellationToken cancellationToken) =>
        repositories.ExecuteGuardedAsync(async () =>
        {
            var actor = await RequireApprovedMemberAsync(userId, cancellationToken);
            var friendship = await repositories.Friendships.GetAsync(requestId, cancellationToken);
            if (friendship is not { Status: ShootingRangeFriendshipStatus.Pending, RequesterUserId: var requesterId } ||
                requesterId != actor.Id || friendship.UserAId != actor.Id && friendship.UserBId != actor.Id)
                throw new InvalidOperationException("Unable to cancel friend request.");
            await repositories.Friendships.DeleteFriendshipAsync(friendship.Id, cancellationToken);
            return true;
        }, cancellationToken);

    public Task RemoveFriendAsync(string userId, string friendId, CancellationToken cancellationToken) =>
        repositories.ExecuteGuardedAsync(async () =>
        {
            var actor = await RequireApprovedMemberAsync(userId, cancellationToken);
            var friendship = (await repositories.Friendships.GetAllAsync(cancellationToken))
                .FirstOrDefault(item =>
                    item.Status == ShootingRangeFriendshipStatus.Accepted &&
                    (item.UserAId == actor.Id && item.UserBId == friendId ||
                     item.UserBId == actor.Id && item.UserAId == friendId));
            if (friendship is null) throw new InvalidOperationException("Unable to remove friend.");
            await repositories.Friendships.DeleteFriendshipAsync(friendship.Id, cancellationToken);
            return true;
        }, cancellationToken);

    public Task CreateAdminBookingAsync(string actorId, AdminBookingRequest request, CancellationToken cancellationToken) =>
        repositories.ExecuteGuardedAsync(async () =>
        {
            await RequireAdminAsync(actorId, cancellationToken);
            var config = await GetConfigAsync(cancellationToken);
            var (start, end) = ResolvePeriod(config, request.LocalDate, request.PeriodKey);
            if (request.ExpectedStartUtc.Kind != DateTimeKind.Utc || request.ExpectedEndUtc.Kind != DateTimeKind.Utc ||
                request.ExpectedStartUtc != start || request.ExpectedEndUtc != end)
                throw new InvalidOperationException("The schedule changed. Refresh sessions before booking.");
            await repositories.Bookings.InsertIfAvailableAsync(new ShootingRangeBooking
            {
                Id = ShootingRangeIds.NewId(),
                UserId = request.UserId,
                BayId = request.BayId,
                LocalDate = request.LocalDate,
                PeriodKey = request.PeriodKey,
                StartUtc = start,
                EndUtc = end,
                Request = request.Request?.Trim() ?? string.Empty,
                Status = ShootingRangeBookingStatus.Approved
            }, cancellationToken);
            return true;
        }, cancellationToken);

    private async Task<ShootingRangeUser> RequireApprovedMemberAsync(string userId, CancellationToken cancellationToken)
    {
        var actor = await repositories.Users.GetAsync(userId, cancellationToken);
        if (actor is null || actor.Status != ShootingRangeAccountStatus.Approved || actor.ForcePasswordChange)
            throw new UnauthorizedAccessException("An approved account without a required password change is necessary.");
        return actor;
    }

    private async Task<HashSet<string>> GetFriendIdsAsync(string userId, CancellationToken cancellationToken)
    {
        var candidateIds = (await repositories.Friendships.GetAllAsync(cancellationToken))
            .Where(friendship => friendship.Status == ShootingRangeFriendshipStatus.Accepted &&
                (friendship.UserAId == userId || friendship.UserBId == userId))
            .Select(friendship => friendship.UserAId == userId ? friendship.UserBId : friendship.UserAId)
            .ToHashSet(StringComparer.Ordinal);
        return (await repositories.Users.GetAllAsync(cancellationToken))
            .Where(user => candidateIds.Contains(user.Id) && user.Status == ShootingRangeAccountStatus.Approved)
            .Select(user => user.Id)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static (string UserAId, string UserBId) OrderPair(string first, string second) =>
        string.CompareOrdinal(first, second) < 0 ? (first, second) : (second, first);

    internal static async Task<bool> IsMutualFriendAsync(
        IShootingRangeRepositorySet repositories, string firstUserId, string secondUserId, CancellationToken cancellationToken)
    {
        var (userAId, userBId) = OrderPair(firstUserId, secondUserId);
        var first = await repositories.Users.GetAsync(firstUserId, cancellationToken);
        var second = await repositories.Users.GetAsync(secondUserId, cancellationToken);
        return first is { Status: ShootingRangeAccountStatus.Approved } &&
            second is { Status: ShootingRangeAccountStatus.Approved } &&
            (await repositories.Friendships.GetAllAsync(cancellationToken))
                .Any(friendship => friendship.Status == ShootingRangeFriendshipStatus.Accepted &&
                    friendship.UserAId == userAId && friendship.UserBId == userBId);
    }

    private async Task<DateOnly> GetFirstVisibleBookingDateAsync(CancellationToken cancellationToken)
    {
        var config = await GetConfigAsync(cancellationToken);
        var zone = TimeZoneInfo.FindSystemTimeZoneById(config.TimeZone);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(repositories.Clock.GetUtcNow(), zone).DateTime);
        return today.AddDays(-1);
    }

    public Task<ShootingRangeBooking> CreateBookingAsync(string userId, BookingRequest request, CancellationToken cancellationToken) => repositories.ExecuteGuardedAsync(async () =>
    {
        var config = await GetConfigAsync(cancellationToken);
        var (start, end) = ResolvePeriod(config, request.LocalDate, request.PeriodKey);
        if (config.SessionMode == ShootingRangeSessionMode.Mixed && (!request.ExpectedStartUtc.HasValue || !request.ExpectedEndUtc.HasValue) ||
            request.ExpectedStartUtc.HasValue != request.ExpectedEndUtc.HasValue ||
            request.ExpectedStartUtc.HasValue && (request.ExpectedStartUtc.Value.Kind != DateTimeKind.Utc ||
                request.ExpectedEndUtc!.Value.Kind != DateTimeKind.Utc ||
                request.ExpectedStartUtc != start || request.ExpectedEndUtc != end))
            throw new InvalidOperationException("The schedule changed. Refresh sessions before booking.");
        var booking = new ShootingRangeBooking { Id = ShootingRangeIds.NewId(), UserId = userId, BayId = request.BayId, LocalDate = request.LocalDate, PeriodKey = request.PeriodKey, StartUtc = start, EndUtc = end, Request = request.Request?.Trim() ?? string.Empty };
        return await repositories.Bookings.InsertIfAvailableAsync(booking, cancellationToken);
    }, cancellationToken);

    internal static async Task ValidateAdmissionAsync(IShootingRangeRepositorySet repositories, ShootingRangeBooking booking, CancellationToken cancellationToken)
    {
        var user = await repositories.Users.GetAsync(booking.UserId, cancellationToken);
        if (user is null || user.Status != ShootingRangeAccountStatus.Approved || user.ForcePasswordChange) throw new UnauthorizedAccessException("An approved account without a required password change is necessary.");
        var config = await LoadConfigAsync(repositories, cancellationToken);
        var (start, end) = ResolvePeriod(config, booking.LocalDate, booking.PeriodKey);
        if (start != booking.StartUtc || end != booking.EndUtc) throw new ArgumentException("Historical interval is not offered by the current schedule.");
        if (start <= repositories.Clock.GetUtcNow().UtcDateTime || !config.OpeningDays.Contains(booking.LocalDate.DayOfWeek)) throw new ArgumentException("The field is closed or the session has already started.");
        var bay = await repositories.Bays.GetAsync(booking.BayId, cancellationToken) ?? throw new KeyNotFoundException("Bay not found.");
        var closures = await repositories.Exceptions.GetAllAsync(cancellationToken);
        if (bay.Status != ShootingRangeBayStatus.Available || closures.Any(closure => closure.LocalDate == booking.LocalDate && closure.IsClosed && (closure.BayId is null || closure.BayId == bay.Id))) throw new ArgumentException("The field or bay is closed.");
        var nowLocal = TimeZoneInfo.ConvertTime(repositories.Clock.GetUtcNow(), TimeZoneInfo.FindSystemTimeZoneById(config.TimeZone));
        var daysUntil = booking.LocalDate.DayNumber - DateOnly.FromDateTime(nowLocal.DateTime).DayNumber;
        if (daysUntil > config.ReservedReleaseDaysBefore && bay.WhitelistUserIds.Count > 0 && !bay.WhitelistUserIds.Contains(booking.UserId)) throw new UnauthorizedAccessException("Bay is reserved until the release threshold.");
    }

    public static (DateTime StartUtc, DateTime EndUtc) ResolvePeriod(ShootingRangeConfig config, DateOnly date, string periodKey)
    {
        var session = GenerateSessions(config, date).SingleOrDefault(session => session.PeriodKey.Equals(periodKey, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException("Session is not offered by the current schedule.");
        return (session.StartUtc, session.EndUtc);
    }

    public static void ValidateConfig(ShootingRangeConfig config)
    {
        if (!Enum.IsDefined(config.SessionMode) ||
            (config.SessionMode == ShootingRangeSessionMode.Mixed
                ? config.HourlyMinutes is not 60 and not 120
                : config.HourlyMinutes != 60) ||
            config.ReservedReleaseDaysBefore < 0 || config.OpeningDays is null || config.OpeningDays.Count == 0 || config.OpeningDays.Any(day => !Enum.IsDefined(day)))
            throw new ArgumentException("Select a valid mode, opening days and supported duration.");
        try { TimeZoneInfo.FindSystemTimeZoneById(config.TimeZone); }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException) { throw new ArgumentException("Invalid field time zone.", exception); }
        static bool ValidWindow(TimeSpan? start, TimeSpan? end) => start.HasValue && end.HasValue && start >= TimeSpan.Zero && end < TimeSpan.FromDays(1) && start < end && start.Value.Ticks % TimeSpan.TicksPerMinute == 0 && end.Value.Ticks % TimeSpan.TicksPerMinute == 0;
        if (config.SessionMode == ShootingRangeSessionMode.HourlyContinuous)
        {
            if (!ValidWindow(config.ContinuousStart, config.ContinuousEnd) || config.ContinuousEnd - config.ContinuousStart < TimeSpan.FromHours(1))
                throw new ArgumentException("Continuous opening must contain at least one full hour.");
        }
        else if (!ValidWindow(config.MorningStart, config.MorningEnd) || !ValidWindow(config.AfternoonStart, config.AfternoonEnd) || config.MorningEnd >= config.AfternoonStart ||
                 config.SessionMode == ShootingRangeSessionMode.Hourly && (config.MorningEnd - config.MorningStart < TimeSpan.FromHours(1) || config.AfternoonEnd - config.AfternoonStart < TimeSpan.FromHours(1)))
            throw new ArgumentException("Morning and afternoon must be ordered with a positive break.");
    }

    public static IReadOnlyList<SessionDto> GenerateSessions(ShootingRangeConfig config, DateOnly date)
    {
        ValidateConfig(config);
        var zone = TimeZoneInfo.FindSystemTimeZoneById(config.TimeZone);
        var sessions = new List<SessionDto>();
        void Add(string key, TimeSpan start, TimeSpan end)
        {
            var localStart = date.ToDateTime(TimeOnly.FromTimeSpan(start), DateTimeKind.Unspecified);
            var localEnd = date.ToDateTime(TimeOnly.FromTimeSpan(end), DateTimeKind.Unspecified);
            if (zone.IsInvalidTime(localStart) || zone.IsInvalidTime(localEnd) || zone.IsAmbiguousTime(localStart) || zone.IsAmbiguousTime(localEnd)) return;
            var startUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, zone);
            var endUtc = TimeZoneInfo.ConvertTimeToUtc(localEnd, zone);
            if (config.SessionMode == ShootingRangeSessionMode.Hourly && endUtc - startUtc != TimeSpan.FromHours(1)) return;
            if (config.SessionMode == ShootingRangeSessionMode.Mixed && endUtc - startUtc != end - start) return;
            if (config.SessionMode == ShootingRangeSessionMode.HourlyContinuous && endUtc - startUtc != TimeSpan.FromHours(1)) return;
            sessions.Add(new(key, startUtc, endUtc));
        }
        void AddHours(TimeSpan start, TimeSpan end)
        {
            for (var slotStart = start; slotStart.Add(TimeSpan.FromHours(1)) <= end; slotStart = slotStart.Add(TimeSpan.FromHours(1)))
                Add(slotStart.ToString(@"hh\:mm", System.Globalization.CultureInfo.InvariantCulture), slotStart, slotStart.Add(TimeSpan.FromHours(1)));
        }
        if (config.SessionMode == ShootingRangeSessionMode.Periods)
        {
            Add("morning", config.MorningStart, config.MorningEnd);
            Add("afternoon", config.AfternoonStart, config.AfternoonEnd);
        }
        else if (config.SessionMode == ShootingRangeSessionMode.Hourly)
        {
            AddHours(config.MorningStart, config.MorningEnd);
            AddHours(config.AfternoonStart, config.AfternoonEnd);
        }
        else if (config.SessionMode == ShootingRangeSessionMode.HourlyContinuous)
            AddHours(config.ContinuousStart!.Value, config.ContinuousEnd!.Value);
        else
        {
            Add("morning", config.MorningStart, config.MorningEnd);
            Add("afternoon", config.AfternoonStart, config.AfternoonEnd);
            AddMixedBlocks(config.MorningStart, config.MorningEnd);
            AddMixedBlocks(config.AfternoonStart, config.AfternoonEnd);
        }
        return sessions;

        void AddMixedBlocks(TimeSpan windowStart, TimeSpan windowEnd)
        {
            var increment = TimeSpan.FromMinutes(config.HourlyMinutes);
            for (var start = windowStart; start.Add(increment) <= windowEnd; start = start.Add(increment))
            {
                for (var end = start.Add(increment); end <= windowEnd; end = end.Add(increment))
                    Add($"{start:hh\\:mm}-{end:hh\\:mm}", start, end);
            }
        }
    }

    internal static bool BlocksBay(ShootingRangeBooking booking, string bayId, DateTime start, DateTime end) => booking.BayId == bayId &&
        (booking.Status is ShootingRangeBookingStatus.Pending or ShootingRangeBookingStatus.Approved) &&
        (booking.StartUtc.Kind != DateTimeKind.Utc || booking.EndUtc.Kind != DateTimeKind.Utc || booking.StartUtc >= booking.EndUtc || booking.StartUtc < end && booking.EndUtc > start);
}
