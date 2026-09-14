using System.ComponentModel.DataAnnotations;
using MorWalPizVideo.ShootingRange.Models;

namespace MorWalPizVideo.ShootingRange.Contracts;

public sealed record RegisterShootingRangeUserRequest([Required, StringLength(80, MinimumLength = 3)] string Username, [Required, StringLength(100, MinimumLength = 12)] string Password, [Required, StringLength(80)] string FirstName, [Required, StringLength(80)] string LastName);
public sealed record ShootingRangeLoginRequest([Required] string Username, [Required] string Password);
public sealed record PasswordChangeRequest([Required] string CurrentPassword, [Required, StringLength(100, MinimumLength = 12)] string NewPassword);
public sealed record AdminPasswordResetRequest([Required, StringLength(100, MinimumLength = 12)] string NewPassword);
public sealed record BookingRequest([Required] string BayId, [Required] DateOnly LocalDate, [Required] string PeriodKey, [StringLength(1000)] string Request);
public sealed record BookingDecisionRequest(bool Approved);
public sealed record ThreadRequest([Required, StringLength(120)] string Subject, [Required, StringLength(2000)] string Text);
public sealed record ThreadMessageRequest([Required, StringLength(2000)] string Text);
public sealed record AccountDto(string Id, string Username, string FirstName, string LastName, ShootingRangeAccountStatus Status, bool IsAdmin, bool ForcePasswordChange);
public sealed record BookingDto(string Id, string BayId, DateOnly LocalDate, string PeriodKey, DateTime StartUtc, DateTime EndUtc, string Request, ShootingRangeBookingStatus Status);
public sealed record AvailabilityDto(DateOnly LocalDate, string PeriodKey, DateTime StartUtc, DateTime EndUtc, IReadOnlyList<ShootingRangeBay> Bays);