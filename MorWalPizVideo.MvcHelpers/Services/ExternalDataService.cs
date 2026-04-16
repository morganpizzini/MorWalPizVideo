using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.Server.Services
{
    public interface IExternalDataService
    {
        Task<IList<YouTubeContent>> FetchMatches();
        Task<YouTubeContent?> RefreshMatch(string id);
    }
  
    public class ExternalDataService : IExternalDataService
    {
        private readonly DataService _dataService;
        private readonly IYTService _youtubeService;
        public ExternalDataService(DataService dataService, IYTService youtubeService)
        {
            _dataService = dataService;
            _youtubeService = youtubeService;
        }
        public async Task<IList<YouTubeContent>> FetchMatches()
        {
            
            IList<YouTubeContent> matches = await _dataService.FetchMatches();

            // Get all videoIds that need to be populated with details
            // For single videos (IsLink=true), fetch if title/description is empty
            // For collections, get all videoIds from the VideoRefs
            var videoIds = new List<string>();
            
            // Add single video IDs that need metadata
            videoIds.AddRange(
                matches
                    .Where(x => x.IsLink && (string.IsNullOrEmpty(x.Title) || string.IsNullOrEmpty(x.Description)))
                    .Select(x => x.ThumbnailVideoId)
            );
            
            // Add collection video ref IDs that need metadata
            videoIds.AddRange(
                matches
                    .Where(x => !x.IsLink && x.VideoRefs != null && x.VideoRefs.Length > 0)
                    .SelectMany(x => x.VideoRefs)
                    .Where(a => string.IsNullOrEmpty(a.Title))
                    .Select(x => x.YoutubeId)
            );
            
            videoIds = videoIds.Distinct().ToList();

            if (videoIds.Count > 0)
            {
                var videos = await _youtubeService.FetchFromYoutube(videoIds);
                matches = ParseMatches(matches, videos);

                // Identify matches that need to be updated in the repository
                var matchesToUpdate = matches
                    .Where(x => x.IsLink && videoIds.Contains(x.ThumbnailVideoId))
                    .Concat(
                        matches
                            .Where(x => !x.IsLink && x.VideoRefs != null)
                            .Where(x => x.VideoRefs.Any(v => videoIds.Contains(v.YoutubeId)))
                    )
                    .ToList();

                foreach (var match in matchesToUpdate)
                {
                    await _dataService.UpdateMatch(match);
                }
            }

            return matches.OrderByDescending(x => x.CreationDateTime).ToList();
        }

        public async Task<YouTubeContent?> RefreshMatch(string id)
        {
            // Find the match by id
            var match = await _dataService.FindMatch(id);
            if (match == null)
            {
                return null;
            }

            // Collect all video IDs that need to be refreshed (force refresh, ignore existing metadata)
            var videoIds = new List<string>();
            
            if (match.IsLink)
            {
                // For single videos, refresh the thumbnail video
                videoIds.Add(match.ThumbnailVideoId);
            }
            else if (match.VideoRefs != null && match.VideoRefs.Length > 0)
            {
                // For collections, refresh all video refs
                videoIds.AddRange(match.VideoRefs.Select(x => x.YoutubeId));
            }

            if (videoIds.Count == 0)
            {
                return match;
            }

            // Fetch fresh YouTube metadata
            var videos = await _youtubeService.FetchFromYoutube(videoIds.Distinct().ToList());
            
            // Parse and update the match
            var updatedMatches = ParseMatches(new[] { match }, videos);
            var updatedMatch = updatedMatches.FirstOrDefault();

            if (updatedMatch != null)
            {
                // Persist the updated match
                await _dataService.UpdateMatch(updatedMatch);
                return updatedMatch;
            }

            return match;
        }

        private static string GenerateUrlSlug(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return string.Empty;
            
            // Trim and replace spaces with dashes
            var slug = title.Replace("-", "").Trim().Replace(" ", "-");
            
            // URL encode to handle special characters
            slug = Uri.EscapeDataString(slug);
            
            return slug;
        }

        private IList<YouTubeContent> ParseMatches(IList<YouTubeContent> matches, IList<Video> videos)
        {
            // Create a dictionary for quick video lookup
            var videoDict = videos.ToDictionary(v => v.YoutubeId, v => v);
            var updatedMatches = new List<YouTubeContent>(matches.Count);

            foreach (var match in matches)
            {
                if (match.IsLink && videoDict.TryGetValue(match.ThumbnailVideoId, out var singleVideo))
                {
                    // For single video matches, update title, description, etc. from the video
                    // Also update the VideoRef with metadata
                    var updatedVideoRef = new VideoRef(
                        singleVideo.YoutubeId,
                        match.VideoRefs?.FirstOrDefault()?.Categories ?? Array.Empty<CategoryRef>(),
                        singleVideo.Title,
                        singleVideo.Description,
                        singleVideo.PublishedAt
                    );
                    
                    // Generate URL from title if current URL is empty
                    var url = string.IsNullOrWhiteSpace(match.Url) 
                        ? GenerateUrlSlug(singleVideo.Title) 
                        : match.Url;
                    
                    var updatedMatch = match with
                    {
                        Title = singleVideo.Title,
                        Description = singleVideo.Description,
                        Url = url,
                        CreationDateTime = singleVideo.PublishedAt,
                        VideoRefs = new[] { updatedVideoRef }
                    };
                    
                    updatedMatches.Add(updatedMatch);
                }
                else if (!match.IsLink && match.VideoRefs?.Length > 0)
                {
                    // For collection matches, load video details for all the refs
                    var videosList = new List<Video>();
                    var updatedVideoRefs = new List<VideoRef>();
                    
                    foreach (var videoRef in match.VideoRefs)
                    {
                        if (videoDict.TryGetValue(videoRef.YoutubeId, out var video))
                        {
                            // Set the category from the ref
                            videosList.Add(video with { Categories = videoRef.Categories });
                            
                            // Create updated VideoRef with metadata from fetched video
                            var updatedVideoRef = new VideoRef(
                                videoRef.YoutubeId,
                                videoRef.Categories,
                                video.Title,
                                video.Description,
                                video.PublishedAt
                            );
                            updatedVideoRefs.Add(updatedVideoRef);
                        }
                        else
                        {
                            // Keep original VideoRef if video data not found
                            updatedVideoRefs.Add(videoRef);
                        }
                    }

                    // Update the match with the new VideoRefs containing metadata
                    var updatedMatch = match with { VideoRefs = updatedVideoRefs.ToArray() };
                    
                    // Update creation date time based on oldest video
                    if (videosList.Count > 0 && videosList.Any(v => v.PublishedAt != DateTime.MinValue))
                    {
                        updatedMatch = updatedMatch with
                        {
                            CreationDateTime = videosList
                                .Where(v => v.PublishedAt != DateTime.MinValue)
                                .Min(v => v.PublishedAt)
                        };
                    }

                    updatedMatches.Add(updatedMatch);
                }
                else
                {
                    // No changes needed
                    updatedMatches.Add(match);
                }
            }

            return updatedMatches;
        }
        //public void Dispose()
        //{
        //    _youtubeService?.Dispose();
        //}
    }

    public interface IShortLinkDataService
    {
        public Task<IList<ShortLink>> FetchShortLink();
        public Task UpdateShortlink(ShortLink entity);
        Task<IList<YouTubeContent>> FetchMatches();
        Task<IList<YTChannel>> FetchChannels();
        Task UpdateYouTubeContent(YouTubeContent entity);
        Task UpdateYTChannel(YTChannel entity);
    }

    public class ShortlinkDataService : IShortLinkDataService
    {
        private readonly IShortLinkRepository _shortLinkRepository;
        private readonly IYouTubeContentRepository _matchRepository;
        private readonly IYTChannelRepository _channelRepository;

        public ShortlinkDataService(
            IYouTubeContentRepository matchRepository,
            IShortLinkRepository shortLinkRepository,
            IYTChannelRepository channelRepository)
        {
            _matchRepository = matchRepository;
            _shortLinkRepository = shortLinkRepository;
            _channelRepository = channelRepository;
        }
        public Task<IList<ShortLink>> FetchShortLink() => _shortLinkRepository.GetItemsAsync();
        public Task UpdateShortlink(ShortLink entity) => _shortLinkRepository.UpdateItemAsync(entity);
        public Task<IList<YouTubeContent>> FetchMatches() => _matchRepository.GetItemsAsync();
        public Task<IList<YTChannel>> FetchChannels() => _channelRepository.GetItemsAsync();
        public Task UpdateYouTubeContent(YouTubeContent entity) => _matchRepository.UpdateItemAsync(entity);
        public Task UpdateYTChannel(YTChannel entity) => _channelRepository.UpdateItemAsync(entity);

        public void Dispose() { }

    }
}
