using MongoDB.Bson.Serialization.Attributes;
using MorWalPizVideo.Server.Models;
using System.Runtime.Serialization;

namespace MorWalPizVideo.Models.Models;

/// <summary>
/// Many-to-Many: tracks which YouTube channels a user follows for push notifications.
/// </summary>
public record UserChannel : BaseEntity
{
  [DataMember]
  [BsonElement("userId")]
  public string UserId { get; init; } = string.Empty;

  [DataMember]
  [BsonElement("channelId")]
  public string ChannelId { get; init; } = string.Empty;

  /// <summary>Whether the subscription is still active.</summary>
  [DataMember]
  [BsonElement("isActive")]
  public bool IsActive { get; init; } = true;
}

/// <summary>
/// Many-to-Many: tracks which users have backoffice management rights over a specific channel.
/// </summary>
public record UserChannelOwner : BaseEntity
{
  [DataMember]
  [BsonElement("userId")]
  public string UserId { get; init; } = string.Empty;

  [DataMember]
  [BsonElement("channelId")]
  public string ChannelId { get; init; } = string.Empty;

  [DataMember]
  [BsonElement("isActive")]
  public bool IsActive { get; init; } = true;
}
