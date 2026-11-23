namespace MorWalPizVideo.ShootingRange.Models;

public enum ShootingRangeAccountStatus { Pending, Approved, Disabled }
public enum ShootingRangeBookingStatus { Pending, Approved, Rejected, Cancelled }
public enum ShootingRangeSessionMode { Periods = 0, Hourly = 1, HourlyContinuous = 2, Mixed = 3 }
public enum ShootingRangeBayStatus { Available, Maintenance, Disabled }
public enum ShootingRangeThreadStatus { Pending, InProgress, Completed, Refused }
public enum ShootingRangeFriendshipStatus { Pending, Accepted }

public sealed record Mechanism(string Type, int Quantity);

public sealed record ShootingRangeTenant : BaseEntity
{
    public string Slug { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string ClientOrigin { get; init; } = string.Empty;
    public bool IsActive { get; init; } = true;
}

public sealed record ShootingRangeMechanismType : BaseEntity
{
    public string Name { get; init; } = string.Empty;
    [System.Text.Json.Serialization.JsonIgnore] public string NormalizedName { get; init; } = string.Empty;
}

public sealed record ShootingRangeConfig : BaseEntity
{
    public string Name { get; init; } = "Pepperbox";
    public string TimeZone { get; init; } = "Europe/Rome";
    public ShootingRangeSessionMode SessionMode { get; init; } = ShootingRangeSessionMode.Periods;
    public List<DayOfWeek> OpeningDays { get; init; } = [DayOfWeek.Saturday, DayOfWeek.Sunday];
    public TimeSpan MorningStart { get; init; } = new(9, 0, 0);
    public TimeSpan MorningEnd { get; init; } = new(13, 0, 0);
    public TimeSpan AfternoonStart { get; init; } = new(14, 0, 0);
    public TimeSpan AfternoonEnd { get; init; } = new(18, 0, 0);
    public int HourlyMinutes { get; init; } = 60;
    public TimeSpan? ContinuousStart { get; init; }
    public TimeSpan? ContinuousEnd { get; init; }
    public int ReservedReleaseDaysBefore { get; init; } = 2;
    [System.Text.Json.Serialization.JsonIgnore] public int MinimumCancellationDays { get; init; }
    [System.Text.Json.Serialization.JsonIgnore] public int PolicyRevision { get; init; }
    [System.Text.Json.Serialization.JsonIgnore] public List<ShootingRangeNoShowRule> NoShowRules { get; init; } = [];
}

public sealed record ShootingRangeNoShowRule(string Id, int OccurrenceThreshold, int WindowDays, string Label);

public sealed record ShootingRangeBay : BaseEntity
{
    public string Code { get; init; } = string.Empty;
    public List<Mechanism> Mechanisms { get; init; } = [];
    [System.Text.Json.Serialization.JsonRequired] public int Targets { get; init; }
    public int Partitions { get; init; }
    public int Plates { get; init; }
    public int Pepper { get; init; }
    [System.Text.Json.Serialization.JsonRequired] public decimal? Height { get; init; }
    [System.Text.Json.Serialization.JsonRequired] public decimal? Width { get; init; }
    public ShootingRangeBayStatus Status { get; init; } = ShootingRangeBayStatus.Available;
    public string Description { get; init; } = string.Empty;
    public DateTime? ReservedUntilUtc { get; init; }
    public List<string> WhitelistUserIds { get; init; } = [];
}

public sealed record ShootingRangeException : BaseEntity
{
    public DateOnly LocalDate { get; init; }
    public string? BayId { get; init; }
    public bool IsClosed { get; init; }
    public string Reason { get; init; } = string.Empty;
}

public sealed record ShootingRangeUser : BaseEntity
{
    public string Username { get; init; } = string.Empty;
    [System.Text.Json.Serialization.JsonIgnore] public string NormalizedUsername { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string PasswordHash { get; init; } = string.Empty;
    public ShootingRangeAccountStatus Status { get; init; } = ShootingRangeAccountStatus.Pending;
    public bool IsAdmin { get; init; }
    public bool ForcePasswordChange { get; init; }
    [System.Text.Json.Serialization.JsonIgnore] public string SecurityVersion { get; init; } = string.Empty;
}

public sealed record ShootingRangeLocalSession : BaseEntity
{
    public string UserId { get; init; } = string.Empty;
    public string SecurityVersion { get; init; } = string.Empty;
    public DateTime ExpiresUtc { get; init; }
    public bool Revoked { get; init; }
}

public sealed record ShootingRangeFriendship : BaseEntity
{
    public string UserAId { get; init; } = string.Empty;
    public string UserBId { get; init; } = string.Empty;
    public ShootingRangeFriendshipStatus Status { get; init; } = ShootingRangeFriendshipStatus.Pending;
    public string RequesterUserId { get; init; } = string.Empty;
}

public sealed record ShootingRangeBooking : BaseEntity
{
    public string UserId { get; init; } = string.Empty;
    public string BayId { get; init; } = string.Empty;
    public DateOnly LocalDate { get; init; }
    public string PeriodKey { get; init; } = string.Empty;
    public DateTime StartUtc { get; init; }
    public DateTime EndUtc { get; init; }
    public string Request { get; init; } = string.Empty;
    public ShootingRangeBookingStatus Status { get; init; } = ShootingRangeBookingStatus.Pending;
    [System.Text.Json.Serialization.JsonIgnore] public bool IsNoShow { get; init; }
    [System.Text.Json.Serialization.JsonIgnore] public DateOnly? NoShowDate { get; init; }
}

public sealed record ShootingRangeThread : BaseEntity
{
    public string UserId { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public ShootingRangeThreadStatus Status { get; init; } = ShootingRangeThreadStatus.Pending;
    public List<ShootingRangeMessage> Messages { get; init; } = [];
}

public sealed record ShootingRangeMessage(string AuthorUserId, bool FromAdmin, string Text, DateTime CreatedUtc);