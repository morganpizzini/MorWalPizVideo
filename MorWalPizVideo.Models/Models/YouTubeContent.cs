using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace MorWalPizVideo.Server.Models
{
    [BsonIgnoreExtraElements]
    [DataContract]
    public record YouTubeContent : BaseEntity
    {
        [JsonConstructor]
        public YouTubeContent(
            string contentId, 
            string title, 
            string description, 
            string url, 
            string thumbnailVideoId, 
            VideoRef[] videoRefs, 
            CategoryRef[]? categories = null, 
            YoutubeContentType contentType = YoutubeContentType.Collection,
            YouTubeVideoLink[]? youtubeVideoLinks = null)
        {
            ContentId = contentId;
            Title = title;
            Description = description;
            Url = url;
            ThumbnailVideoId = thumbnailVideoId;
            VideoRefs = videoRefs;
            Categories = categories ?? Array.Empty<CategoryRef>();
            ContentType = contentType;
            YouTubeVideoLinks = youtubeVideoLinks ?? Array.Empty<YouTubeVideoLink>();
        }
        [DataMember]
        [BsonElement("contentId")]
        public string ContentId { get; init; }

        [DataMember]
        [BsonElement("title")]
        public string Title { get; init; }

        [DataMember]
        [BsonElement("description")]
        public string Description { get; init; }

        [DataMember]
        [BsonElement("url")]
        public string Url { get; init; }

        [DataMember]
        [BsonElement("thumbnailVideoId")]
        public string ThumbnailVideoId { get; init; }

        [DataMember]
        [BsonElement("videoRefs")]
        public VideoRef[] VideoRefs { get; init; }

        [DataMember]
        [BsonElement("categories")]
        public CategoryRef[] Categories { get; init; } = Array.Empty<CategoryRef>();

        [DataMember]
        [BsonElement("contentType")]
        public YoutubeContentType ContentType { get; init; } = YoutubeContentType.Collection;

        [DataMember]
        [BsonElement("youtubeVideoLinks")]
        public YouTubeVideoLink[]? YouTubeVideoLinks { get; init; } = Array.Empty<YouTubeVideoLink>();

        [DataMember]
        [BsonElement("shortLinks")]
        public ShortLink[] ShortLinks { get; init; } = Array.Empty<ShortLink>();

        // Backward compatibility property - the Match is considered a direct video link if it's a SingleVideo type
        [BsonIgnore]
        public bool IsLink => ContentType == YoutubeContentType.SingleVideo;
        
        // Constructor for a single video match
        public static YouTubeContent CreateSingleVideo(string videoId, CategoryRef[] categories)
        {
            return new YouTubeContent(
                videoId, // The Match ID is the video ID for single videos
                string.Empty,
                string.Empty, 
                string.Empty, 
                videoId, // The thumbnail video is the same as the video ID
                new[] { new VideoRef(videoId, categories) }, 
                categories,
                YoutubeContentType.SingleVideo);
        }
        
        // Constructor for creating an empty collection that will hold multiple videos
        public static YouTubeContent CreateCollection(string id, string title, string description, string url, string thumbnailVideoId, CategoryRef[] categories)
        {
            return new YouTubeContent(
                id,
                title,
                description,
                url,
                thumbnailVideoId,
                Array.Empty<VideoRef>(),
                categories,
                YoutubeContentType.Collection);
        }
        
        // For backward compatibility with existing code
        public YouTubeContent(string thumbnailUrl, bool isLink, CategoryRef[] categories) : this(
            thumbnailUrl, 
            string.Empty, 
            string.Empty, 
            string.Empty, 
            thumbnailUrl, 
            isLink ? new[] { new VideoRef(thumbnailUrl, categories) } : Array.Empty<VideoRef>(), 
            categories,
            isLink ? YoutubeContentType.SingleVideo : YoutubeContentType.Collection)
        {
        }
        
        // Add a video to the collection
        public YouTubeContent AddVideo(string videoId, CategoryRef[] categories)
        {
            var newVideoRefs = VideoRefs.Append(new VideoRef(videoId, categories)).ToArray();
            return this with { VideoRefs = newVideoRefs };
        }
        
        // Add a video to the collection with metadata
        public YouTubeContent AddVideo(string videoId, CategoryRef[] categories, string title, string description, DateOnly publishedAt)
        {
            var newVideoRefs = VideoRefs.Append(new VideoRef(videoId, categories, title, description, publishedAt)).ToArray();
            return this with { VideoRefs = newVideoRefs };
        }
        
        // Remove a video from the collection
        public YouTubeContent RemoveVideo(string videoId)
        {
            var newVideoRefs = VideoRefs.Where(v => v.YoutubeId != videoId).ToArray();
            return this with { VideoRefs = newVideoRefs };
        }
        
        // Change the thumbnail video
        public YouTubeContent WithThumbnail(string newThumbnailVideoId)
        {
            return this with { ThumbnailVideoId = newThumbnailVideoId };
        }
        
        public YouTubeContent AddYouTubeVideoLink(YouTubeVideoLink videoLink)
        {
            var newVideoLinks = (YouTubeVideoLinks ?? Array.Empty<YouTubeVideoLink>()).Append(videoLink).ToArray();
            return this with { YouTubeVideoLinks = newVideoLinks };
        }
        
        // Remove a YouTube video link from the collection
        public YouTubeContent RemoveYouTubeVideoLink(string youtubeVideoId)
        {
            var newVideoLinks = (YouTubeVideoLinks ?? Array.Empty<YouTubeVideoLink>()).Where(v => v.YouTubeVideoId != youtubeVideoId).ToArray();
            return this with { YouTubeVideoLinks = newVideoLinks };
        }
        
        // Update a YouTube video link in the collection
        public YouTubeContent UpdateYouTubeVideoLink(string youtubeVideoId, YouTubeVideoLink updatedVideoLink)
        {
            var newVideoLinks = (YouTubeVideoLinks ?? Array.Empty<YouTubeVideoLink>()).Select(v => 
                v.YouTubeVideoId == youtubeVideoId ? updatedVideoLink : v).ToArray();
            return this with { YouTubeVideoLinks = newVideoLinks };
        }
        
        // Add a shortlink to the collection
        public YouTubeContent AddShortLink(ShortLink shortLink)
        {
            var newShortLinks = ShortLinks.Append(shortLink).ToArray();
            return this with { ShortLinks = newShortLinks };
        }
        
        // Remove a shortlink from the collection
        public YouTubeContent RemoveShortLink(string code)
        {
            var newShortLinks = ShortLinks.Where(sl => sl.Code != code).ToArray();
            return this with { ShortLinks = newShortLinks };
        }
        
        // Update a shortlink in the collection
        public YouTubeContent UpdateShortLink(string code, ShortLink updatedShortLink)
        {
            var newShortLinks = ShortLinks.Select(sl => 
                sl.Code == code ? updatedShortLink : sl).ToArray();
            return this with { ShortLinks = newShortLinks };
        }
        
        // Get a shortlink by code
        public ShortLink? GetShortLink(string code)
        {
            return ShortLinks.FirstOrDefault(sl => sl.Code == code);
        }
        
        // Convert Match to VideoDisplayItem for UI
        public VideoDisplayItem ToDisplayItem()
        {
            if (ContentType == YoutubeContentType.SingleVideo)
            {
                return VideoDisplayItem.CreateSingleVideo(
                    ThumbnailVideoId, 
                    Title, 
                    Description, 
                    ThumbnailVideoId, 
                    Categories);
            }
            else
            {
                // Get all video IDs from the VideoRefs
                var videoIds = VideoRefs.Select(v => v.YoutubeId).ToArray();
                
                return VideoDisplayItem.CreatePlaylist(
                    Id,
                    Title,
                    Description,
                    ThumbnailVideoId,
                    Categories,
                    ThumbnailVideoId,
                    videoIds);
            }
        }
        
        // Create Match from VideoDisplayItem
        public static YouTubeContent FromDisplayItem(VideoDisplayItem item)
        {
            if (item.ContentType == Models.ContentType.SingleVideo)
            {
                return CreateSingleVideo(item.PrimaryVideoId, item.Categories);
            }
            else
            {
                var content = CreateCollection(
                    item.DisplayId,
                    item.Title,
                    item.Description,
                    string.Empty, // No URL in VideoDisplayItem
                    item.ThumbnailUrl,
                    item.Categories);
                
                // Add all videos from VideoIds
                foreach (var videoId in item.VideoIds)
                {
                    content = content.AddVideo(videoId, item.Categories);
                }
                
                return content;
            }
        }
    }
      
    // Enum to clearly represent the type of match
    public enum YoutubeContentType
    {
        SingleVideo, // A match representing a single video (previously IsLink=true)
        Collection    // A match representing a collection of videos (previously IsLink=false)
    }
}
