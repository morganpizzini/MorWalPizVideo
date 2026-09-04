using MorWalPizVideo.Server.Models;
using System.Runtime.Serialization;

namespace MorWalPiz.Contracts.Contracts;

[DataContract]
public sealed class AskCampaignContract
{
    [DataMember] public string Id { get; set; } = string.Empty;
    [DataMember] public string ChannelId { get; set; } = string.Empty;
    [DataMember] public string Title { get; set; } = string.Empty;
    [DataMember] public string Description { get; set; } = string.Empty;
    [DataMember] public string Slug { get; set; } = string.Empty;
    [DataMember] public AskCampaignStatus Status { get; set; }
    [DataMember] public DateTime? StartAt { get; set; }
    [DataMember] public DateTime? EndAt { get; set; }
    [DataMember] public DateTime? PublishedAt { get; set; }
    [DataMember] public DateTime? ClosedAt { get; set; }
    [DataMember] public AskPolicy Policy { get; set; } = new();
    [DataMember] public int SubmissionCount { get; set; }
}

[DataContract]
public sealed class AskPublicCampaignContract
{
    [DataMember] public string ChannelName { get; set; } = string.Empty;
    [DataMember] public string Title { get; set; } = string.Empty;
    [DataMember] public string Description { get; set; } = string.Empty;
    [DataMember] public string Slug { get; set; } = string.Empty;
    [DataMember] public AskCampaignStatus Status { get; set; } = AskCampaignStatus.Published;
    [DataMember] public DateTime? StartAt { get; set; }
    [DataMember] public DateTime? EndAt { get; set; }
    [DataMember] public int MaxSubmissionLength { get; set; }
    [DataMember] public bool AllowNamedSubmissions { get; set; }
    [DataMember] public bool NameRequired { get; set; }
    [DataMember] public bool RecaptchaRequired { get; set; }
    [DataMember] public IReadOnlyList<AskPublicResponseContract> Responses { get; set; } = [];
    [DataMember] public IReadOnlyList<AskPublicQuestionContract> Questions { get; set; } = [];
}

[DataContract]
public sealed class AskPublicResponseContract
{
    [DataMember] public string Content { get; set; } = string.Empty;
    [DataMember] public string Author { get; set; } = string.Empty;
    [DataMember] public DateTime CreatedAt { get; set; }
}

[DataContract]
public sealed class AskSubmissionContract
{
    [DataMember] public string Id { get; set; } = string.Empty;
    [DataMember] public string CampaignId { get; set; } = string.Empty;
    [DataMember] public string Text { get; set; } = string.Empty;
    [DataMember] public string Name { get; set; } = string.Empty;
    [DataMember] public AskModerationStatus ModerationStatus { get; set; }
    [DataMember] public DateTime SubmittedAt { get; set; }
    [DataMember] public string ResponseContent { get; set; } = string.Empty;
    [DataMember] public string ResponseAuthor { get; set; } = string.Empty;
    [DataMember] public DateTime? ResponseCreatedAt { get; set; }
    [DataMember] public AskResponseVisibility? ResponseVisibility { get; set; }
    [DataMember] public int ReactionCount { get; set; }
}

[DataContract]
public sealed class AskResponseRequest
{
    [DataMember] public string Content { get; set; } = string.Empty;
    [DataMember] public string Author { get; set; } = string.Empty;
    [DataMember] public AskResponseVisibility Visibility { get; set; }
}

[DataContract]
public sealed class AskReactionContract
{
    [DataMember] public int Count { get; set; }
    [DataMember] public bool Accepted { get; set; }
}

[DataContract]
public sealed class AskPublicQuestionContract
{
    [DataMember] public string Id { get; set; } = string.Empty;
    [DataMember] public string Text { get; set; } = string.Empty;
    [DataMember] public int ReactionCount { get; set; }
    [DataMember] public AskPublicResponseContract? Response { get; set; }
}