using MongoDB.Bson.Serialization.Attributes;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Models.Models;

public sealed record PublicAccount : BaseEntity
{
    [BsonElement("username")]
    public string Username { get; init; } = string.Empty;

    [BsonElement("normalizedUsername")]
    public string NormalizedUsername { get; init; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; init; } = string.Empty;

    [BsonElement("normalizedEmail")]
    public string NormalizedEmail { get; init; } = string.Empty;

    [BsonElement("displayName")]
    public string DisplayName { get; init; } = string.Empty;

    [BsonElement("passwordHash")]
    public string PasswordHash { get; init; } = string.Empty;

    [BsonElement("salt")]
    public string Salt { get; init; } = string.Empty;

    [BsonElement("isActive")]
    public bool IsActive { get; init; } = true;

    [BsonElement("securityVersion")]
    public int SecurityVersion { get; init; }
}

public sealed record PublicAccountSession : BaseEntity
{
    [BsonElement("accountId")]
    public string AccountId { get; init; } = string.Empty;

    [BsonElement("tokenHash")]
    public string TokenHash { get; init; } = string.Empty;

    [BsonElement("securityVersion")]
    public int SecurityVersion { get; init; }

    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; init; }

    [BsonElement("revokedAt")]
    public DateTime? RevokedAt { get; init; }
}
