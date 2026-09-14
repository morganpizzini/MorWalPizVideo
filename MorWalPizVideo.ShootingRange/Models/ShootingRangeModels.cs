using MongoDB.Bson.Serialization.Attributes;

namespace MorWalPizVideo.ShootingRange.Models;

public enum ShootingRangeAccountStatus { Pending, Approved, Disabled }
public enum ShootingRangeBookingStatus { Pending, Approved, Rejected, Cancelled }
public enum ShootingRangeSessionMode { Periods, Hourly }
public enum ShootingRangeBayStatus { Available, Maintenance, Disabled }

public sealed record Mechanism(string Type, int Quantity);

public sealed record ShootingRangeConfig : BaseEntity
{
    [BsonElement("name")] public string Name { get; init; } = "Pepperbox";
    [BsonElement("timeZone")] public string TimeZone { get; init; } = "Europe/Rome";
    [BsonElement("sessionMode")] public ShootingRangeSessionMode SessionMode { get; init; } = ShootingRangeSessionMode.Periods;
    [BsonElement("openingDays")] public List<DayOfWeek> OpeningDays { get; init; } = [DayOfWeek.Saturday, DayOfWeek.Sunday];
    [BsonElement("morningStart")] public TimeSpan MorningStart { get; init; } = new(9, 0, 0);
    [BsonElement("morningEnd")] public TimeSpan MorningEnd { get; init; } = new(13, 0, 0);
    [BsonElement("afternoonStart")] public TimeSpan AfternoonStart { get; init; } = new(14, 0, 0);
    [BsonElement("afternoonEnd")] public TimeSpan AfternoonEnd { get; init; } = new(18, 0, 0);
    [BsonElement("hourlyMinutes")] public int HourlyMinutes { get; init; } = 60;
    [BsonElement("reservedReleaseDaysBefore")] public int ReservedReleaseDaysBefore { get; init; } = 2;
}

public sealed record ShootingRangeBay : BaseEntity
{
    [BsonElement("code")] public string Code { get; init; } = string.Empty;
    [BsonElement("mechanisms")] public List<Mechanism> Mechanisms { get; init; } = [];
    [BsonElement("partitions")] public int Partitions { get; init; }
    [BsonElement("plates")] public int Plates { get; init; }
    [BsonElement("pepper")] public bool Pepper { get; init; }
    [BsonElement("status")] public ShootingRangeBayStatus Status { get; init; } = ShootingRangeBayStatus.Available;
    [BsonElement("description")] public string Description { get; init; } = string.Empty;
    [BsonElement("reservedUntilUtc")] public DateTime? ReservedUntilUtc { get; init; }
    [BsonElement("whitelistUserIds")] public List<string> WhitelistUserIds { get; init; } = [];
}

public sealed record ShootingRangeException : BaseEntity
{
    [BsonElement("localDate")] public DateOnly LocalDate { get; init; }
    [BsonElement("bayId")] public string? BayId { get; init; }
    [BsonElement("isClosed")] public bool IsClosed { get; init; }
    [BsonElement("reason")] public string Reason { get; init; } = string.Empty;
}

public sealed record ShootingRangeUser : BaseEntity
{
    [BsonElement("username")] public string Username { get; init; } = string.Empty;
    [BsonElement("firstName")] public string FirstName { get; init; } = string.Empty;
    [BsonElement("lastName")] public string LastName { get; init; } = string.Empty;
    [BsonElement("passwordHash")] public string PasswordHash { get; init; } = string.Empty;
    [BsonElement("status")] public ShootingRangeAccountStatus Status { get; init; } = ShootingRangeAccountStatus.Pending;
    [BsonElement("isAdmin")] public bool IsAdmin { get; init; }
    [BsonElement("forcePasswordChange")] public bool ForcePasswordChange { get; init; }
}

public sealed record ShootingRangeBooking : BaseEntity
{
    [BsonElement("userId")] public string UserId { get; init; } = string.Empty;
    [BsonElement("bayId")] public string BayId { get; init; } = string.Empty;
    [BsonElement("localDate")] public DateOnly LocalDate { get; init; }
    [BsonElement("periodKey")] public string PeriodKey { get; init; } = string.Empty;
    [BsonElement("startUtc")] public DateTime StartUtc { get; init; }
    [BsonElement("endUtc")] public DateTime EndUtc { get; init; }
    [BsonElement("request")] public string Request { get; init; } = string.Empty;
    [BsonElement("status")] public ShootingRangeBookingStatus Status { get; init; } = ShootingRangeBookingStatus.Pending;
}

public sealed record ShootingRangeThread : BaseEntity
{
    [BsonElement("userId")] public string UserId { get; init; } = string.Empty;
    [BsonElement("subject")] public string Subject { get; init; } = string.Empty;
    [BsonElement("messages")] public List<ShootingRangeMessage> Messages { get; init; } = [];
}

public sealed record ShootingRangeMessage(string AuthorUserId, bool FromAdmin, string Text, DateTime CreatedUtc);