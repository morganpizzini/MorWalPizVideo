using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace MorWalPizVideo.Server.Models
{
    [BsonIgnoreExtraElements]
    [DataContract]
    public record YTChannel(
        [property: DataMember][property: BsonElement("channelId")] string ChannelId,
        [property: DataMember][property: BsonElement("channelName")] string ChannelName) : BaseEntity
    {
        [DataMember]
        [BsonElement("socials")]
        public List<ChannelSocial> Socials { get; init; } = [];
        [DataMember]
        [BsonElement("shortLinkUrl")]
        public string ShortLinkUrl { get; init; } = string.Empty;

        [DataMember]
        [BsonElement("videos")]
        public List<YouTubeVideo> Videos { get; init; } = new List<YouTubeVideo>();

        [DataMember]
        [BsonElement("shortLinks")]
        public ShortLink[] ShortLinks { get; init; } = Array.Empty<ShortLink>();

        [DataMember]
        [BsonElement("isSHIT")]
        [JsonPropertyName("isSHIT")]
        public bool IsSHIT { get; init; } = false;

        [DataMember]
        [BsonElement("channelLogoStorageKey")]
        public string ChannelLogoStorageKey { get; init; } = string.Empty;

        [DataMember]
        [BsonElement("channelLogoUrl")]
        public string ChannelLogoUrl { get; init; } = string.Empty;
        
        // Add a shortlink to the collection
        public YTChannel AddShortLink(ShortLink shortLink)
        {
            var newShortLinks = ShortLinks.Append(shortLink).ToArray();
            return this with { ShortLinks = newShortLinks };
        }
        
        // Remove a shortlink from the collection
        public YTChannel RemoveShortLink(string code)
        {
            var newShortLinks = ShortLinks.Where(sl => !sl.MatchesCode(code)).ToArray();
            return this with { ShortLinks = newShortLinks };
        }
        
        // Update a shortlink in the collection
        public YTChannel UpdateShortLink(string code, ShortLink updatedShortLink)
        {
            var newShortLinks = ShortLinks.Select(sl => 
                sl.MatchesCode(code) ? updatedShortLink : sl).ToArray();
            return this with { ShortLinks = newShortLinks };
        }
        
        // Get a shortlink by code
        public ShortLink? GetShortLink(string code)
        {
            return ShortLinks.FirstOrDefault(sl => sl.MatchesCode(code));
        }
    }

    [BsonIgnoreExtraElements]
    [DataContract]
    public record ChannelSocial
    {
        [DataMember]
        [BsonElement("provider")]
        public string Provider { get; init; } = string.Empty;

        [DataMember]
        [BsonElement("handler")]
        public string Handler { get; init; } = string.Empty;
    }

    [BsonIgnoreExtraElements]
    [DataContract]
    public record YouTubeVideo
    {
        [DataMember]
        [BsonElement("videoId")]
        public string VideoId { get; init; } = string.Empty;

        [DataMember]
        [BsonElement("title")]
        public string Title { get; init; } = string.Empty;

        [DataMember]
        [BsonElement("lastCommentDate")]
        public DateTime LastCommentDate { get; init; } = DateTime.MinValue;

        [DataMember]
        [BsonElement("videoIdeas")]
        public List<VideoIdea> VideoIdeas { get; init; } = new List<VideoIdea>();
    }

    [BsonIgnoreExtraElements]
    [DataContract]
    public record VideoIdea
    {
        [DataMember]
        [BsonElement("idea")]
        public string Idea { get; init; } = string.Empty;

        [DataMember]
        [BsonElement("commentExcerpt")]
        public string CommentExcerpt { get; init; } = string.Empty;

        [DataMember]
        [BsonElement("creationDate")]
        public DateTime CreationDate { get; init; } = DateTime.Now;

        [DataMember]
        [BsonElement("sentiment")]
        public string Sentiment { get; init; } = string.Empty;
    }
}
