using Microsoft.Extensions.Configuration;
using MorWalPizVideo.Server.Contracts;
using MorWalPizVideo.Server.Models;
using System.Net.Http;
using System.Text.Json;
using System.Web;

namespace MorWalPizVideo.Server.Services
{
    public class ExternalDataService
    {
        private readonly IConfiguration _configuration;
        private readonly DataService _dataService;
        private readonly IHttpClientFactory _httpClientFactory;
        public ExternalDataService(IConfiguration configuration, DataService dataService, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _dataService = dataService;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IList<Match>> FetchMatches()
        {
            var matches = _dataService.GetItems();

            var httpClient = _httpClientFactory.CreateClient("Youtube");

            var query = HttpUtility.ParseQueryString(string.Empty);

            query["part"] = "id,snippet,statistics,contentDetails";
            query["id"] = string.Join(",", matches.Where(x => x.Videos != null).SelectMany(x => x.Videos).Select(x => x.Id).ToList().Concat(matches.Select(m => m.ThumbnailUrl).ToList()));
            query["key"] = _configuration["YTApiKey"];
            string queryString = query.ToString() ?? "";

            var httpResponseMessage = await httpClient.GetAsync($"?{queryString}");
            if (!httpResponseMessage.IsSuccessStatusCode)
                return matches;

            using var contentStream =
                await httpResponseMessage.Content.ReadAsStreamAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
            var youtubeResponse = await JsonSerializer.DeserializeAsync<VideoResponse>(contentStream, options);

            if (youtubeResponse == null)
                return matches;

            foreach (var item in youtubeResponse.Items)
            {
                var video = ContractUtils.Convert(item);

                var match = matches.FirstOrDefault(x => x.Videos != null && x.Videos.Any(v => v.Id == video.Id));
                if (match != null)
                {
                    // is related video
                    var index = Array.FindIndex(match.Videos, x => x.Id == video.Id);
                    match.Videos[index] = video with { Category = match.Videos[index].Category };
                }
                else
                {
                    //is home page video
                    var element = matches.FirstOrDefault(x => x.ThumbnailUrl == video.Id);
                    if (element == null)
                        continue;
                    var index = matches.IndexOf(element);

                    matches[index] = element with { Title = video.Title, Description = video.Description, CreationDateTime = video.PublishedAt.ToDateTime(TimeOnly.MinValue), Url = video.Id };
                }
            }

            return (matches.Select(match =>
                    match.Videos?.Length > 0
                        ? match with { CreationDateTime = match.Videos.Min(x => x.PublishedAt).ToDateTime(TimeOnly.MinValue) }
                        : match
                ).ToList()).OrderByDescending(x=>x.CreationDateTime).ToList();
        }
    }
}
