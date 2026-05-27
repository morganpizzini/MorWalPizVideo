using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Models.Models;

public record User : BaseEntity
{
    [BsonElement("username")]
    public string Username { get; init; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; init; } = string.Empty;

    [BsonElement("passwordHash")]
    public string PasswordHash { get; init; } = string.Empty;

    [BsonElement("salt")]
    public string Salt { get; init; } = string.Empty;

    [BsonElement("lastLogin")]
    public DateTime? LastLogin { get; init; }

    [BsonElement("isActive")]
    public bool IsActive { get; init; } = true;

    [BsonElement("role")]
    public string Role { get; init; } = "User";

    /// <summary>Flag to grant access to the BackOffice admin interface.</summary>
    [BsonElement("canAccessBackoffice")]
    public bool CanAccessBackoffice { get; init; } = false;

    /// <summary>List of Web Push subscription endpoints/keys for browser push notifications.</summary>
    [BsonElement("pushSubscriptions")]
    public List<PushSubscriptionInfo> PushSubscriptions { get; init; } = new();
}

/// <summary>Stores the data needed to send a Web Push notification to a browser endpoint.</summary>
public record PushSubscriptionInfo
{
    [BsonElement("endpoint")]
    public string Endpoint { get; init; } = string.Empty;

    [BsonElement("p256dh")]
    public string P256dh { get; init; } = string.Empty;

    [BsonElement("auth")]
    public string Auth { get; init; } = string.Empty;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
