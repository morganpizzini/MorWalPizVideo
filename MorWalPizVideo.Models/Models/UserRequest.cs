using MongoDB.Bson.Serialization.Attributes;
using MorWalPizVideo.Server.Models;
using System.Runtime.Serialization;

namespace MorWalPizVideo.Models.Models;

/// <summary>
/// Stores community requests for new video topics ("Di cosa vorresti che parlassimo?").
/// Managed/moderated by admins in the backoffice.
/// </summary>
public record UserRequest : BaseEntity
{
  [DataMember]
  [BsonElement("name")]
  public string Name { get; init; } = string.Empty;

  [DataMember]
  [BsonElement("email")]
  public string Email { get; init; } = string.Empty;

  [DataMember]
  [BsonElement("topic")]
  public string Topic { get; init; } = string.Empty;

  [DataMember]
  [BsonElement("description")]
  public string? Description { get; init; }

  [DataMember]
  [BsonElement("status")]
  public UserRequestStatus Status { get; init; } = UserRequestStatus.Pending;

  /// <summary>Admin note / grouping label.</summary>
  [DataMember]
  [BsonElement("adminNote")]
  public string? AdminNote { get; init; }

  /// <summary>Number of upvotes / similar requests grouped together.</summary>
  [DataMember]
  [BsonElement("votes")]
  public int Votes { get; init; } = 1;
}

public enum UserRequestStatus
{
  Pending = 0,
  UnderReview = 1,
  InProduction = 2,
  Completed = 3,
  Rejected = 4
}
