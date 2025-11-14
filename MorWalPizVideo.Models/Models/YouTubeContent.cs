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
            string category = "", 
            YoutubeContentType contentType = YoutubeContentType.Collection,
            YouTubeVideoLink[]? youtubeVideoLinks = null)
        {
            ContentId = contentId;
            Title = title;
            Description = description;
            Url = url;
            ThumbnailVideoId = thumbnailVideoId;
            VideoRefs = videoRefs;
            Category = category;
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
        [BsonElement("category")]
        public string Category { get; init; } = "";

        [DataMember]
        [BsonElement("contentType")]
        public YoutubeContentType ContentType { get; init; } = YoutubeContentType.Collection;

        [DataMember]
        [BsonElement("youtubeVideoLinks")]
        public YouTubeVideoLink[]? YouTubeVideoLinks { get; init; } = Array.Empty<YouTubeVideoLink>();

        // Backward compatibility property - the Match is considered a direct video link if it's a SingleVideo type
        [BsonIgnore]
        public bool IsLink => ContentType == YoutubeContentType.SingleVideo;
        
        // Constructor for a single video match
        public static YouTubeContent CreateSingleVideo(string videoId, string category)
        {
            return new YouTubeContent(
                videoId, // The Match ID is the video ID for single videos
                string.Empty, 
                string.Empty, 
                string.Empty, 
                videoId, // The thumbnail video is the same as the video ID
                new[] { new VideoRef(videoId, category) }, 
                category,
                YoutubeContentType.SingleVideo);
        }
        
        // Constructor for creating an empty collection that will hold multiple videos
        public static YouTubeContent CreateCollection(string id, string title, string description, string url, string thumbnailVideoId, string category)
        {
            return new YouTubeContent(
                id,
                title,
                description,
                url,
                thumbnailVideoId,
                Array.Empty<VideoRef>(),
                category,
                YoutubeContentType.Collection);
        }
        
        // For backward compatibility with existing code
        public YouTubeContent(string thumbnailUrl, bool isLink, string category) : this(
            thumbnailUrl, 
            string.Empty, 
            string.Empty, 
            string.Empty, 
            thumbnailUrl, 
            isLink ? new[] { new VideoRef(thumbnailUrl, category) } : Array.Empty<VideoRef>(), 
            category,
            isLink ? YoutubeContentType.SingleVideo : YoutubeContentType.Collection)
        {
        }
        
        // Add a video to the collection
        public YouTubeContent AddVideo(string videoId, string category)
        {
            var newVideoRefs = VideoRefs.Append(new VideoRef(videoId, category)).ToArray();
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
                    Category);
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
                    Category,
                    ThumbnailVideoId,
                    videoIds);
            }
        }
        
        // Create Match from VideoDisplayItem
        public static YouTubeContent FromDisplayItem(VideoDisplayItem item)
        {
            if (item.ContentType == Models.ContentType.SingleVideo)
            {
                return CreateSingleVideo(item.PrimaryVideoId, item.Category);
            }
            else
            {
                var content = CreateCollection(
                    item.DisplayId,
                    item.Title,
                    item.Description,
                    string.Empty, // No URL in VideoDisplayItem
                    item.ThumbnailUrl,
                    item.Category);
                
                // Add all videos from VideoIds
                foreach (var videoId in item.VideoIds)
                {
                    content = content.AddVideo(videoId, item.Category);
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
