using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.Server.Services
{
    public interface IExternalDataService : IDisposable
    {
        Task<IList<YouTubeContent>> FetchMatches();
    }
  
    public class ExternalDataService : IExternalDataService
    {
        private readonly IYouTubeContentRepository _matchRepository;
        private readonly IYTService _youtubeService;
        public ExternalDataService(IYTService youtubeService, IYouTubeContentRepository matchRepository)
        {
            _matchRepository = matchRepository;
            _youtubeService = youtubeService;
        }
        public async Task<IList<YouTubeContent>> FetchMatches()
        {
            IList<YouTubeContent> matches = await _matchRepository.GetItemsAsync();

            // Get all videoIds that need to be populated with details
            // For single videos, use the ThumbnailVideoId
            // For collections, get all videoIds from the VideoRefs
            var videoIds = matches
                .Where(x => x.IsLink && string.IsNullOrEmpty(x.Title))
                .Select(x => x.ThumbnailVideoId)
                .Concat(
                    matches
                        .Where(x => !x.IsLink && x.VideoRefs != null && x.VideoRefs.Length > 0)
                        .SelectMany(x => x.VideoRefs)
                        .Select(x => x.YoutubeId)
                )
                .Distinct()
                .ToList();

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
                    
                    await _matchRepository.UpdateItemAsync(match);
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
                        CreationDateTime = singleVideo.PublishedAt.ToDateTime(TimeOnly.MinValue)
                    };
                    
                    updatedMatches.Add(updatedMatch);
                }
                else if (!match.IsLink && match.VideoRefs?.Length > 0)
                {
                    // For collection matches, load video details for all the refs
                    var videosList = new List<Video>();
                    foreach (var videoRef in match.VideoRefs)
                    {
                        if (videoDict.TryGetValue(videoRef.YoutubeId, out var video))
                        {
                            // Set the category from the ref
                            videosList.Add(video with { Category = videoRef.Category });
                        }
                    }

                    // Set the Videos property for compatibility
                    var updatedMatch = match;
                    
                    // Update creation date time based on oldest video
                    if (videosList.Count > 0 && videosList.Any(v => v.PublishedAt != DateOnly.MinValue))
                    {
                        updatedMatch = updatedMatch with
                        {
                            CreationDateTime = videosList
                                .Where(v => v.PublishedAt != DateOnly.MinValue)
                                .Min(v => v.PublishedAt.ToDateTime(TimeOnly.MinValue))
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
        public void Dispose()
        {
            _youtubeService?.Dispose();
        }
    }

    public interface IShortLinkDataService : IExternalDataService
    {
        public Task<ShortLink?> GetShortLink(string shortLink);
        public Task UpdateShortlink(ShortLink entity);
        
    }

    public class ShortlinkDataService : IShortLinkDataService
    {
        private readonly IShortLinkRepository _shortLinkRepository;
        private readonly IYouTubeContentRepository _matchRepository;

        public ShortlinkDataService(
            IYouTubeContentRepository matchRepository,
            IShortLinkRepository shortLinkRepository)
        {
            _matchRepository = matchRepository;
            _shortLinkRepository = shortLinkRepository;
        }
        public async Task<ShortLink?> GetShortLink(string shortLink) => (await _shortLinkRepository.GetItemsAsync(x => x.Code.ToLower() == shortLink.ToLower())).FirstOrDefault();
        public Task UpdateShortlink(ShortLink entity) => _shortLinkRepository.UpdateItemAsync(entity);
        public Task<IList<YouTubeContent>> FetchMatches() => _matchRepository.GetItemsAsync();

        public void Dispose() { }

    }
}
