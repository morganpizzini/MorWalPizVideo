using MorWalPizVideo.Server.Contracts;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;
using System.Text.Json;
using System.Web;

namespace MorWalPizVideo.Server.Services
{
    public interface IExternalDataService
    {
        Task<IList<Match>> FetchMatches();
    }
    public class ExternalDataMockService : IExternalDataService
    {
        private readonly DataService _dataService;
        public ExternalDataMockService(DataService dataService)
        {
            _dataService = dataService;
        }

        public Task<IList<Match>> FetchMatches()
        {
            return _dataService.GetItems();
        }
    }

    public class ExternalDataService : IExternalDataService
    {
        private readonly DataService _dataService;
        private readonly IConfiguration _configuration;

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMatchRepository _matchRepository;
        public ExternalDataService(IConfiguration configuration, DataService dataService, IHttpClientFactory httpClientFactory, IMatchRepository matchRepository)
        {
            _dataService = dataService;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _matchRepository = matchRepository;
        }

        public async Task<IList<Match>> FetchMatches()
        {
            IList<Match> matches = await _dataService.GetItems();

            var videoIds = matches.Where(x => x.IsLink && string.IsNullOrEmpty(x.Title)).Select(x => x.ThumbnailUrl)
                .Concat(
                    matches.Where(x => x.Videos != null && x.Videos.Length != 0).SelectMany(x => x.Videos).Where(x => string.IsNullOrEmpty(x.Title)).Select(x => x.YoutubeId).ToList()
                ).ToList();

            if (videoIds.Count > 0)
            {
                var videos = await FetchFromYoutube(videoIds);
                matches = ParseMatches(matches, videos);

                var linkVideos = matches.Where(x => x.IsLink && videoIds.Contains(x.ThumbnailUrl))
                    .Concat(
                        matches.Where(x => x.Videos != null && x.Videos.Length != 0)
                                .Where(x => x.Videos.Any(v => videoIds.Contains(v.YoutubeId)))
                    ).ToList();

                foreach (var linkVideo in linkVideos)
                {
                    await _matchRepository.UpdateItemAsync(linkVideo);
                }
            }

            return matches.OrderByDescending(x => x.CreationDateTime).ToList();
        }
        private async Task<IList<Video>> FetchFromYoutube(IList<string> videoIds)
        {
            var videos = new List<Video>();
            if (videoIds.Count == 0)
                return videos;
            using var httpClient = _httpClientFactory.CreateClient("Youtube");

            var query = HttpUtility.ParseQueryString(string.Empty);

            query["part"] = "id,snippet,statistics,contentDetails";
            query["id"] = string.Join(",", videoIds);
            query["key"] = _configuration["YTApiKey"];
            string queryString = query.ToString() ?? "";

            var httpResponseMessage = await httpClient.GetAsync($"?{queryString}");
            if (!httpResponseMessage.IsSuccessStatusCode)
                return videos;

            using var contentStream =
                await httpResponseMessage.Content.ReadAsStreamAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
            var youtubeResponse = await JsonSerializer.DeserializeAsync<VideoResponse>(contentStream, options);

            if (youtubeResponse == null)
                return videos;

            return youtubeResponse.Items.Select(ContractUtils.Convert).ToList();
        }
        private IList<Match> ParseMatches(IList<Match> matches, IList<Video> videos)
        {
            // update video entities based on id
            foreach (var video in videos)
            {
                var match = matches.FirstOrDefault(x => x.Videos != null && x.Videos.Any(v => v.YoutubeId == video.YoutubeId));
                if (match != null)
                {
                    var index = Array.FindIndex(match.Videos, x => x.YoutubeId == video.YoutubeId);
                    match.Videos[index] = video with { Category = match.Videos[index].Category };
                }
                else
                {
                    var element = matches.FirstOrDefault(x => x.ThumbnailUrl == video.YoutubeId);
                    if (element == null)
                        continue;
                    var index = matches.IndexOf(element);

                    matches[index] = element with { Title = video.Title, Description = video.Description, CreationDateTime = video.PublishedAt.ToDateTime(TimeOnly.MinValue), Url = video.YoutubeId };
                }
            }

            return matches.Select(match =>
                    match.Videos?.Length > 0
                        ? match with { CreationDateTime = match.Videos.Min(x => x.PublishedAt).ToDateTime(TimeOnly.MinValue) }
                        : match
                ).ToList();
        }

    }
}
