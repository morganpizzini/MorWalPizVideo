using MorWalPizVideo.Server.Models;
using System.Runtime.Serialization;

namespace MorWalPiz.Contracts.Contracts;

[DataContract]
public sealed class FaqPublicContract
{
    [DataMember] public string Id { get; set; } = string.Empty;
    [DataMember] public string Question { get; set; } = string.Empty;
    [DataMember] public string CategorySlug { get; set; } = string.Empty;
    [DataMember] public string CategoryName { get; set; } = string.Empty;
    [DataMember] public IReadOnlyList<FaqPublicAnswerContract> Answers { get; set; } = [];
}
[DataContract]
public sealed class FaqPublicAnswerContract
{
    [DataMember] public string ChannelName { get; set; } = string.Empty;
    [DataMember] public string Content { get; set; } = string.Empty;
    [DataMember] public int HelpfulVotes { get; set; }
    [DataMember] public int NotHelpfulVotes { get; set; }
}
[DataContract]
public sealed class FaqContract
{
    [DataMember] public string Id { get; set; } = string.Empty;
    [DataMember] public string Question { get; set; } = string.Empty;
    [DataMember] public string CategoryId { get; set; } = string.Empty;
    [DataMember] public FaqLifecycleStatus Status { get; set; }
    [DataMember] public DateTime? PublishedAt { get; set; }
    [DataMember] public DateTime UpdatedAt { get; set; }
    [DataMember] public FaqSourceMetadata SourceMetadata { get; set; } = new();
}
[DataContract]
public sealed class FaqAnswerContract
{
    [DataMember] public string Id { get; set; } = string.Empty;
    [DataMember] public string FaqId { get; set; } = string.Empty;
    [DataMember] public string ChannelId { get; set; } = string.Empty;
    [DataMember] public string Content { get; set; } = string.Empty;
    [DataMember] public FaqLifecycleStatus Status { get; set; }
    [DataMember] public int HelpfulVotes { get; set; }
    [DataMember] public int NotHelpfulVotes { get; set; }
}
[DataContract]
public sealed class FaqCategoryContract
{
    [DataMember] public string Id { get; set; } = string.Empty;
    [DataMember] public string Slug { get; set; } = string.Empty;
    [DataMember] public string Name { get; set; } = string.Empty;
    [DataMember] public string Description { get; set; } = string.Empty;
    [DataMember] public int SortOrder { get; set; }
    [DataMember] public bool IsActive { get; set; }
}
[DataContract]
public sealed class FaqVoteContract
{
    [DataMember] public bool Accepted { get; set; }
    [DataMember] public bool Changed { get; set; }
    [DataMember] public int HelpfulVotes { get; set; }
    [DataMember] public int NotHelpfulVotes { get; set; }
}