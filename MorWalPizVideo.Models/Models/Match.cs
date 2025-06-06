using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace MorWalPizVideo.Server.Models
{
    [BsonIgnoreExtraElements]
    [DataContract]
    public record Match : BaseEntity
    {
        [JsonConstructor]
        public Match(
            string id, 
            string title, 
            string description, 
            string url, 
            string thumbnailVideoId, 
            VideoRef[] videoRefs, 
            string category = "", 
            MatchType matchType = MatchType.Collection,
            YouTubeVideoLink[]? youtubeVideoLinks = null)
        {
            Id = id;
            Title = title;
            Description = description;
            Url = url;
            ThumbnailVideoId = thumbnailVideoId;
            VideoRefs = videoRefs;
            Category = category;
            MatchType = matchType;
            YouTubeVideoLinks = youtubeVideoLinks ?? Array.Empty<YouTubeVideoLink>();
        }

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
        [BsonElement("matchType")]
        public MatchType MatchType { get; init; } = MatchType.Collection;

        [DataMember]
        [BsonElement("youtubeVideoLinks")]
        public YouTubeVideoLink[]? YouTubeVideoLinks { get; init; } = Array.Empty<YouTubeVideoLink>();

        [DataMember]
        [BsonElement("matchId")]
        public string MatchId => Id;

        // Backward compatibility property - the Match is considered a direct video link if it's a SingleVideo type
        [BsonIgnore]
        public bool IsLink => MatchType == MatchType.SingleVideo;
        
        // Backward compatibility property - the thumbnail URL is stored as a video ID
        [BsonIgnore]
        public string ThumbnailUrl => ThumbnailVideoId;
        
        // Backward compatibility property - provides access to videos
        [BsonIgnore]
        public Video[] Videos { get; set; } = Array.Empty<Video>();
        
        // Constructor for a single video match
        public static Match CreateSingleVideo(string videoId, string category)
        {
            return new Match(
                videoId, // The Match ID is the video ID for single videos
                string.Empty, 
                string.Empty, 
                string.Empty, 
                videoId, // The thumbnail video is the same as the video ID
                new[] { new VideoRef(videoId, category) }, 
                category,
                MatchType.SingleVideo);
        }
        
        // Constructor for creating an empty collection that will hold multiple videos
        public static Match CreateCollection(string id, string title, string description, string url, string thumbnailVideoId, string category)
        {
            return new Match(
                id,
                title,
                description,
                url,
                thumbnailVideoId,
                Array.Empty<VideoRef>(),
                category,
                MatchType.Collection);
        }
        
        // For backward compatibility with existing code
        public Match(string thumbnailUrl, bool isLink, string category) : this(
            thumbnailUrl, 
            string.Empty, 
            string.Empty, 
            string.Empty, 
            thumbnailUrl, 
            isLink ? new[] { new VideoRef(thumbnailUrl, category) } : Array.Empty<VideoRef>(), 
            category,
            isLink ? MatchType.SingleVideo : MatchType.Collection)
        {
        }
        
        // Add a video to the collection
        public Match AddVideo(string videoId, string category)
        {
            var newVideoRefs = VideoRefs.Append(new VideoRef(videoId, category)).ToArray();
            return this with { VideoRefs = newVideoRefs };
        }
        
        // Remove a video from the collection
        public Match RemoveVideo(string videoId)
        {
            var newVideoRefs = VideoRefs.Where(v => v.YoutubeId != videoId).ToArray();
            return this with { VideoRefs = newVideoRefs };
        }
        
        // Change the thumbnail video
        public Match WithThumbnail(string newThumbnailVideoId)
        {
            return this with { ThumbnailVideoId = newThumbnailVideoId };
        }
        
        public Match AddYouTubeVideoLink(YouTubeVideoLink videoLink)
        {
            var newVideoLinks = (YouTubeVideoLinks ?? Array.Empty<YouTubeVideoLink>()).Append(videoLink).ToArray();
            return this with { YouTubeVideoLinks = newVideoLinks };
        }
        
        // Remove a YouTube video link from the collection
        public Match RemoveYouTubeVideoLink(string youtubeVideoId)
        {
            var newVideoLinks = (YouTubeVideoLinks ?? Array.Empty<YouTubeVideoLink>()).Where(v => v.YouTubeVideoId != youtubeVideoId).ToArray();
            return this with { YouTubeVideoLinks = newVideoLinks };
        }
        
        // Update a YouTube video link in the collection
        public Match UpdateYouTubeVideoLink(string youtubeVideoId, YouTubeVideoLink updatedVideoLink)
        {
            var newVideoLinks = (YouTubeVideoLinks ?? Array.Empty<YouTubeVideoLink>()).Select(v => 
                v.YouTubeVideoId == youtubeVideoId ? updatedVideoLink : v).ToArray();
            return this with { YouTubeVideoLinks = newVideoLinks };
        }
        
        // Convert Match to VideoDisplayItem for UI
        public VideoDisplayItem ToDisplayItem()
        {
            if (MatchType == MatchType.SingleVideo)
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
        public static Match FromDisplayItem(VideoDisplayItem item)
        {
            if (item.ContentType == ContentType.SingleVideo)
            {
                return CreateSingleVideo(item.PrimaryVideoId, item.Category);
            }
            else
            {
                var match = CreateCollection(
                    item.DisplayId,
                    item.Title,
                    item.Description,
                    string.Empty, // No URL in VideoDisplayItem
                    item.ThumbnailUrl,
                    item.Category);
                
                // Add all videos from VideoIds
                foreach (var videoId in item.VideoIds)
                {
                    match = match.AddVideo(videoId, item.Category);
                }
                
                return match;
            }
        }
    }
      
    // Enum to clearly represent the type of match
    public enum MatchType
    {
        SingleVideo, // A match representing a single video (previously IsLink=true)
        Collection    // A match representing a collection of videos (previously IsLink=false)
    }
}
