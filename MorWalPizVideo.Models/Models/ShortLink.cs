using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;

namespace MorWalPizVideo.Server.Models
{
    [DataContract]
    public enum ShortLinkRequestCategory
    {
        [EnumMember]
        SocialPreview = 0,

        [EnumMember]
        OtherAutomation = 1,

        [EnumMember]
        HumanCandidate = 2,

        [EnumMember]
        Unclassified = 3
    }

    [DataContract]
    public sealed record ShortLinkRequestClassification(
        [property: DataMember] ShortLinkRequestCategory Category,
        [property: DataMember] string RuleCode,
        [property: DataMember] string Version);

    [BsonIgnoreExtraElements]
    [DataContract]
    public sealed record ShortLinkRequestEvent : BaseEntity
    {
        [DataMember]
        [BsonElement("shortLinkId")]
        public string ShortLinkId { get; init; } = string.Empty;

        [DataMember]
        [BsonElement("timestamp")]
        public DateTime Timestamp { get; init; }

        [DataMember]
        [BsonElement("category")]
        public ShortLinkRequestCategory Category { get; init; }

        [DataMember]
        [BsonElement("ruleCode")]
        public string RuleCode { get; init; } = string.Empty;

        [DataMember]
        [BsonElement("classifierVersion")]
        public string ClassifierVersion { get; init; } = string.Empty;

        [DataMember]
        [BsonElement("expiresAt")]
        public DateTime ExpiresAt { get; init; }
    }

    [DataContract]
    public enum LinkType
    {
        [EnumMember]
        YouTubeVideo = 0,

        [EnumMember]
        YouTubeChannel = 1,

        [EnumMember]
        YouTubePlaylist = 2,

        [EnumMember]
        Instagram = 3,

        [EnumMember]
        Facebook = 4,

        [EnumMember]
        CustomUrl = 5,
        AskCampaign = 6
    }

    [BsonIgnoreExtraElements]
    [DataContract]
    public record ShortLink(
        [property: DataMember][property: BsonElement("code")] string Code,
        [property: DataMember][property: BsonElement("target")] string Target,
        [property: DataMember][property: BsonElement("queryLinks")] IList<QueryLink> QueryLinks) : BaseEntity
    {
        public static string NormalizeCode(string? code) => (code ?? string.Empty).Trim().ToLowerInvariant();

        public string NormalizedCode => NormalizeCode(Code);

        public bool MatchesCode(string? candidateCode) =>
            string.Equals(NormalizedCode, NormalizeCode(candidateCode), System.StringComparison.OrdinalIgnoreCase);

        [DataMember]
        [BsonElement("clicksCount")]
        public int ClicksCount { get; set; }

        [DataMember]
        [BsonElement("clickTimestamps")]
        public IList<DateTime> ClickTimestamps { get; set; } = [];

        [DataMember]
        [BsonElement("socialPreviewRequests")]
        public int SocialPreviewRequests { get; set; }

        [DataMember]
        [BsonElement("otherAutomationRequests")]
        public int OtherAutomationRequests { get; set; }

        [DataMember]
        [BsonElement("humanCandidateRequests")]
        public int HumanCandidateRequests { get; set; }

        [DataMember]
        [BsonElement("unclassifiedRequests")]
        public int UnclassifiedRequests { get; set; }

        [DataMember]
        [BsonElement("linkType")]
        public LinkType LinkType { get; set; } = LinkType.YouTubeVideo;

        // Canonical internal reference to the owning YouTubeContent, set when this standalone
        // record represents a YouTubeVideo short link (ADR-004: canonical standalone aggregate).
        [DataMember]
        [BsonElement("contentId")]
        public string? ContentId { get; set; }

        // Canonical internal reference to the owning YTChannel, set when this standalone
        // record represents a YouTubeChannel short link (ADR-004: canonical standalone aggregate).
        [DataMember]
        [BsonElement("channelId")]
        public string? ChannelId { get; set; }

        [DataMember]
        [BsonElement("managementChannelId")]
        public string? ManagementChannelId { get; set; }

        [DataMember]
        [BsonElement("campaignId")]
        public string? CampaignId { get; set; }

        [DataMember]
        [BsonElement("sponsorId")]
        public string? SponsorId { get; set; }

        [BsonIgnore]
        public string QueryString => QueryLinks != null ? string.Join("&", QueryLinks.Select(ql => ql.Value)) : string.Empty;
    }
}
