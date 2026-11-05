using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Services.Interfaces;
using System.Globalization;
using System.Text;

namespace MorWalPizVideo.Server.Services
{
    public interface IExternalDataService
    {
        Task<IList<YouTubeContent>> FetchMatches();
        Task<YouTubeContent?> RefreshMatch(string id);
    }
  
    public class ExternalDataService : IExternalDataService
    {
        private readonly IGenericDataService _dataService;
        private readonly IYTService _youtubeService;
        public ExternalDataService(IGenericDataService dataService, IYTService youtubeService)
        {
            _dataService = dataService;
            _youtubeService = youtubeService;
        }
        public async Task<IList<YouTubeContent>> FetchMatches()
        {
            
            IList<YouTubeContent> matches = await _dataService.FetchMatches();

            var videoIds = matches
                .SelectMany(match => (match.VideoRefs ?? [])
                    .Where(video => string.IsNullOrEmpty(video.Title) || string.IsNullOrEmpty(video.Description) ||
                        (IsProviderOwnedRoot(match) &&
                         (string.IsNullOrEmpty(match.Title) || string.IsNullOrEmpty(match.Description))))
                    .Select(video => video.YoutubeId))
                .Distinct()
                .ToList();

            if (videoIds.Count > 0)
            {
                var videos = await _youtubeService.FetchFromYoutube(videoIds);
                matches = ParseMatches(matches, videos);

                var matchesToUpdate = matches
                    .Where(match => (match.VideoRefs ?? []).Any(video => videoIds.Contains(video.YoutubeId)))
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

            var videoIds = (match.VideoRefs ?? []).Select(video => video.YoutubeId).Distinct().ToList();

            if (videoIds.Count == 0)
            {
                return match;
            }

            // Fetch fresh YouTube metadata
            var videos = await _youtubeService.FetchFromYoutube(videoIds);
            
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

            var normalized = title.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);
            var separatorPending = false;

            foreach (var character in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (character is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9')
                {
                    if (separatorPending && builder.Length > 0)
                    {
                        builder.Append('-');
                    }

                    builder.Append(char.ToLowerInvariant(character));
                    separatorPending = false;
                }
                else
                {
                    separatorPending = builder.Length > 0;
                }
            }

            return builder.ToString();
        }

        private static bool IsProviderOwnedRoot(YouTubeContent match) =>
            match.IsLink && string.Equals(match.ContentId, match.VideoRefs[0].YoutubeId, StringComparison.Ordinal);

        private IList<YouTubeContent> ParseMatches(IList<YouTubeContent> matches, IList<Video> videos)
        {
            // Create a dictionary for quick video lookup
            var videoDict = videos.ToDictionary(v => v.YoutubeId, v => v);
            var updatedMatches = new List<YouTubeContent>(matches.Count);

            foreach (var match in matches)
            {
                var publishedDates = new List<DateTime>();
                var updatedVideoRefs = (match.VideoRefs ?? []).Select(videoRef =>
                {
                    if (!videoDict.TryGetValue(videoRef.YoutubeId, out var video))
                        return videoRef;

                    if (video.PublishedAt != DateTime.MinValue)
                        publishedDates.Add(video.PublishedAt);

                    return videoRef with
                    {
                        Title = video.Title,
                        Description = video.Description,
                        PublishedAt = video.PublishedAt
                    };
                }).ToArray();
                var updatedMatch = match with { VideoRefs = updatedVideoRefs };

                // Presentation cardinality does not transfer ownership of editorial metadata.
                if (IsProviderOwnedRoot(match) && videoDict.TryGetValue(match.VideoRefs[0].YoutubeId, out var singleVideo))
                {
                    updatedMatch = updatedMatch with
                    {
                        Title = singleVideo.Title,
                        Description = singleVideo.Description,
                        Url = string.IsNullOrWhiteSpace(match.Url) ? GenerateUrlSlug(singleVideo.Title) : match.Url,
                        CreationDateTime = singleVideo.PublishedAt
                    };
                }
                else if (publishedDates.Count > 0)
                {
                    updatedMatch = updatedMatch with
                    {
                        CreationDateTime = publishedDates.Min()
                    };
                }

                updatedMatches.Add(updatedMatch);
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
        // Canonical-first indexed lookup (ADR-004).
        Task<ShortLink?> GetShortLinkByCode(string code);
        // Atomic click counter for canonical standalone short links.
        Task<int> IncrementShortLinkClicksAsync(string id);
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
        public Task<ShortLink?> GetShortLinkByCode(string code) => _shortLinkRepository.GetByCodeAsync(code);
        public Task<int> IncrementShortLinkClicksAsync(string id) => _shortLinkRepository.IncrementClicksAsync(id);
        public Task<IList<YouTubeContent>> FetchMatches() => _matchRepository.GetItemsAsync();
        public Task<IList<YTChannel>> FetchChannels() => _channelRepository.GetItemsAsync();
        public Task UpdateYouTubeContent(YouTubeContent entity) => _matchRepository.UpdateItemAsync(entity);
        public Task UpdateYTChannel(YTChannel entity) => _channelRepository.UpdateItemAsync(entity);

        public void Dispose() { }

    }
}
