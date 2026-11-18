using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MorWalPizVideo.ShootingRange.Models;

namespace MorWalPizVideo.ShootingRange.Migration.LegacyMongo;

public abstract record LegacyBaseEntity
{
    [BsonId]
    [BsonSerializer(typeof(LegacyRangeIdSerializer))]
    public string Id { get; init; } = string.Empty;

    [BsonElement("creationDateTime")]
    public DateTime CreationDateTime { get; init; } = DateTime.Now;
}

public sealed class LegacyRangeIdSerializer : SerializerBase<string>
{
    public override string Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
        context.Reader.GetCurrentBsonType() == BsonType.ObjectId
            ? context.Reader.ReadObjectId().ToString()
            : context.Reader.ReadString();

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, string value)
    {
        if (ObjectId.TryParse(value, out var objectId))
            context.Writer.WriteObjectId(objectId);
        else
            context.Writer.WriteString(value);
    }
}

public sealed record LegacyMechanism(string Type, int Quantity);

public sealed record LegacyConfig : LegacyBaseEntity
{
    [BsonElement("name")] public string Name { get; init; } = "Pepperbox";
    [BsonElement("timeZone")] public string TimeZone { get; init; } = "Europe/Rome";
    [BsonElement("sessionMode")] public ShootingRangeSessionMode SessionMode { get; init; }
    [BsonElement("openingDays")] public List<DayOfWeek> OpeningDays { get; init; } = [DayOfWeek.Saturday, DayOfWeek.Sunday];
    [BsonElement("morningStart")] public TimeSpan MorningStart { get; init; } = new(9, 0, 0);
    [BsonElement("morningEnd")] public TimeSpan MorningEnd { get; init; } = new(13, 0, 0);
    [BsonElement("afternoonStart")] public TimeSpan AfternoonStart { get; init; } = new(14, 0, 0);
    [BsonElement("afternoonEnd")] public TimeSpan AfternoonEnd { get; init; } = new(18, 0, 0);
    [BsonElement("hourlyMinutes")] public int HourlyMinutes { get; init; } = 60;
    [BsonElement("continuousStart")] public TimeSpan? ContinuousStart { get; init; }
    [BsonElement("continuousEnd")] public TimeSpan? ContinuousEnd { get; init; }
    [BsonElement("reservedReleaseDaysBefore")] public int ReservedReleaseDaysBefore { get; init; } = 2;
}

public sealed record LegacyBay : LegacyBaseEntity
{
    [BsonElement("code")] public string Code { get; init; } = string.Empty;
    [BsonElement("mechanisms")] public List<LegacyMechanism> Mechanisms { get; init; } = [];
    [BsonElement("partitions")] public int Partitions { get; init; }
    [BsonElement("plates")] public int Plates { get; init; }
    [BsonElement("pepper")] public bool Pepper { get; init; }
    [BsonElement("status")] public ShootingRangeBayStatus Status { get; init; }
    [BsonElement("description")] public string Description { get; init; } = string.Empty;
    [BsonElement("reservedUntilUtc")] public DateTime? ReservedUntilUtc { get; init; }
    [BsonElement("whitelistUserIds")] public List<string> WhitelistUserIds { get; init; } = [];
}

public sealed record LegacyException : LegacyBaseEntity
{
    [BsonElement("localDate")] public DateOnly LocalDate { get; init; }
    [BsonElement("bayId")] public string? BayId { get; init; }
    [BsonElement("isClosed")] public bool IsClosed { get; init; }
    [BsonElement("reason")] public string Reason { get; init; } = string.Empty;
}

public sealed record LegacyUser : LegacyBaseEntity
{
    [BsonElement("username")] public string Username { get; init; } = string.Empty;
    [BsonElement("normalizedUsername")] public string NormalizedUsername { get; init; } = string.Empty;
    [BsonElement("firstName")] public string FirstName { get; init; } = string.Empty;
    [BsonElement("lastName")] public string LastName { get; init; } = string.Empty;
    [BsonElement("passwordHash")] public string PasswordHash { get; init; } = string.Empty;
    [BsonElement("status")] public ShootingRangeAccountStatus Status { get; init; }
    [BsonElement("isAdmin")] public bool IsAdmin { get; init; }
    [BsonElement("forcePasswordChange")] public bool ForcePasswordChange { get; init; }
    [BsonElement("securityVersion")] public string SecurityVersion { get; init; } = string.Empty;
}

public sealed record LegacyLocalSession : LegacyBaseEntity
{
    [BsonElement("userId")] public string UserId { get; init; } = string.Empty;
    [BsonElement("securityVersion")] public string SecurityVersion { get; init; } = string.Empty;
    [BsonElement("expiresUtc")] public DateTime ExpiresUtc { get; init; }
    [BsonElement("revoked")] public bool Revoked { get; init; }
}

public sealed record LegacyBooking : LegacyBaseEntity
{
    [BsonElement("userId")] public string UserId { get; init; } = string.Empty;
    [BsonElement("bayId")] public string BayId { get; init; } = string.Empty;
    [BsonElement("localDate")] public DateOnly LocalDate { get; init; }
    [BsonElement("periodKey")] public string PeriodKey { get; init; } = string.Empty;
    [BsonElement("startUtc")] public DateTime StartUtc { get; init; }
    [BsonElement("endUtc")] public DateTime EndUtc { get; init; }
    [BsonElement("request")] public string Request { get; init; } = string.Empty;
    [BsonElement("status")] public ShootingRangeBookingStatus Status { get; init; }
}

public sealed record LegacyThread : LegacyBaseEntity
{
    [BsonElement("userId")] public string UserId { get; init; } = string.Empty;
    [BsonElement("subject")] public string Subject { get; init; } = string.Empty;
    [BsonElement("messages")] public List<LegacyMessage> Messages { get; init; } = [];
}

public sealed record LegacyMessage(string AuthorUserId, bool FromAdmin, string Text, DateTime CreatedUtc);
