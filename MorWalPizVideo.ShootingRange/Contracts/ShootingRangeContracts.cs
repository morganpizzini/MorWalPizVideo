using System.ComponentModel.DataAnnotations;
using MorWalPizVideo.ShootingRange.Models;

namespace MorWalPizVideo.ShootingRange.Contracts;

public sealed record RegisterShootingRangeUserRequest([Required, StringLength(80, MinimumLength = 3)] string Username, [Required, StringLength(100, MinimumLength = 7)] string Password, [Required, StringLength(80)] string FirstName, [Required, StringLength(80)] string LastName);
public sealed record ShootingRangeLoginRequest([Required] string Username, [Required] string Password);
public sealed record PasswordChangeRequest([Required] string CurrentPassword, [Required, StringLength(100, MinimumLength = 7)] string NewPassword);
public sealed record AdminPasswordResetRequest([Required, StringLength(100, MinimumLength = 7)] string NewPassword);
public sealed record BookingRequest([Required] string BayId, [Required] DateOnly LocalDate, [Required] string PeriodKey, [StringLength(1000)] string Request, DateTime? ExpectedStartUtc = null, DateTime? ExpectedEndUtc = null);
public sealed record BookingDecisionRequest(bool Approved);
public sealed record AdminBookingRequest([Required] string UserId, [Required] string BayId, [Required] DateOnly LocalDate, [Required] string PeriodKey, [StringLength(1000)] string Request, [Required] DateTime ExpectedStartUtc, [Required] DateTime ExpectedEndUtc);
public sealed record CreateFriendRequest([Required, StringLength(200)] string Username);
public sealed record CreateMechanismTypeRequest([Required, StringLength(80, MinimumLength = 1)] string Name);
public sealed record ThreadRequest([Required, StringLength(120)] string Subject, [Required, StringLength(2000)] string Text);
public sealed record ThreadMessageRequest([Required, StringLength(2000)] string Text);
public sealed record ThreadStatusDecisionRequest([Required] ShootingRangeThreadStatus? Status);
public sealed record AccountDto(string Id, string Username, string FirstName, string LastName, ShootingRangeAccountStatus Status, bool IsAdmin, bool ForcePasswordChange);
public sealed record BookingDto(string Id, string BayId, DateOnly LocalDate, string PeriodKey, DateTime StartUtc, DateTime EndUtc, string Request, ShootingRangeBookingStatus Status);
public sealed record FriendBookingDto(string BayId, string Username, ShootingRangeBookingStatus Status);
public sealed record FriendDto(string Id, string Username, string FirstName, string LastName);
public sealed record FriendRequestDto(string Id, string Username, string FirstName, string LastName, DateTime CreatedUtc);
public sealed record FriendRequestsDto(IReadOnlyList<FriendRequestDto> Incoming, IReadOnlyList<FriendRequestDto> Outgoing);
public sealed record AvailabilityDto(DateOnly LocalDate, string PeriodKey, DateTime StartUtc, DateTime EndUtc, IReadOnlyList<ShootingRangeBay> Bays, IReadOnlyList<FriendBookingDto> FriendBookings);
public sealed record SessionDto(string PeriodKey, DateTime StartUtc, DateTime EndUtc);
public sealed record SessionsDto(DateOnly LocalDate, string TimeZone, IReadOnlyList<SessionDto> Sessions);
public sealed record AdminUserDto(string Id, DateTime CreationDateTime, string Username, string FirstName, string LastName, ShootingRangeAccountStatus Status, bool IsAdmin, bool ForcePasswordChange);
public sealed record MechanismTypeDto(string Id, string Name);
public sealed record PendingBookingCountDto(int Count);
public sealed record CreateShootingRangeTenantRequest(
    [Required, StringLength(64, MinimumLength = 3), RegularExpression("^[a-z0-9](?:[a-z0-9-]{1,62}[a-z0-9])$")] string Slug,
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [Required, StringLength(300)] string ClientOrigin);
public sealed record ShootingRangeTenantDto(string Id, string Slug, string Name, string ClientOrigin, bool IsActive);