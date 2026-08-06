using MongoDB.Bson.Serialization.Attributes;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Models.Models;

public record UserGroup : BaseEntity
{
  [BsonElement("code")]
  public string Code { get; init; } = string.Empty;

  [BsonElement("name")]
  public string Name { get; init; } = string.Empty;

  [BsonElement("description")]
  public string Description { get; init; } = string.Empty;

  [BsonElement("isActive")]
  public bool IsActive { get; init; } = true;

  [BsonElement("permissions")]
  public List<string> Permissions { get; init; } = new();
}
