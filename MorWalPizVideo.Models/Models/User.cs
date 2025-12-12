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
}
