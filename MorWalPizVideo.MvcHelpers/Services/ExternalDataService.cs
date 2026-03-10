using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.Server.Services
{
    public interface IExternalDataService
    {
        Task<IList<YouTubeContent>> FetchMatches();
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
            // For single videos, use the isLink
            // For collections, get all videoIds from the VideoRefs
            var videoIds =
                    matches
                        .Where(x => !x.IsLink && x.VideoRefs != null && x.VideoRefs.Length > 0)
                        .SelectMany(x => x.VideoRefs)
                        // Only fetch details for VideoRefs that don't have title populated yet
                        .Where(a => string.IsNullOrEmpty(a.Title))
                        .Select(x => x.YoutubeId).ToList();

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
                    var updatedMatch = match with
                    {
                        Title = singleVideo.Title,
                        Description = singleVideo.Description,
                        CreationDateTime = singleVideo.PublishedAt
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
