
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Configuration;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Contracts;
using MorWalPizVideo.Server.Utils;
using System.Text;
using System.Text.Json;
using System.Web;
using System.Xml;
using Video = MorWalPizVideo.Server.Models.Video;

namespace MorWalPizVideo.Server.Services
{
    public interface IYTService : IDisposable
    {
        Task TranslateYoutubeVideo(IList<string> videoIds);
        Task<IList<Video>> FetchFromYoutube(IList<string> videoIds);
        Task<string> GetChannelComments(string channelId, int videoCount = 10, int commentCount = 20, bool showVideo = true);
        Task<string> GetChannelId(string channelName);
    }

    public class YTServiceMock : IYTService
    {
        public void Dispose()
        {
        }
        public Task TranslateYoutubeVideo(IList<string> videoIds) => Task.CompletedTask;
        public Task<IList<Video>> FetchFromYoutube(IList<string> videoIds) => Task.FromResult<IList<Video>>(new List<Video>());
        public Task<string> GetChannelComments(string channelId, int videoCount = 10, int commentCount = 20, bool showVideo= true) => Task.FromResult("");
        public Task<string> GetChannelId(string channelName) => Task.FromResult(string.Empty);
    }

    public class YTService : IYTService
    {
        private readonly HttpClient _client;
        private readonly YouTubeService _youtubeService;
        private readonly YouTubeService _youtubeAuthService;
        private readonly ITranslatorService _translatorService;
        private readonly string _apiKey;
        public YTService(IConfiguration configuration, IHttpClientFactory httpClientFactory, ITranslatorService translatorService)
        {
            _translatorService = translatorService;
            _apiKey = configuration["YTApiKey"] ?? string.Empty;
            _client = httpClientFactory.CreateClient(HttpClientNames.YouTube);
            //_youtubeService = new YouTubeService(new BaseClientService.Initializer
            //{
            //    ApiKey = _apiKey,
            //    ApplicationName = GetType().ToString()
            //});

            //using var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read);

            var credential = GoogleCredential.FromFile("credentials.json")
               .CreateScoped(YouTubeService.Scope.YoutubeForceSsl);

            //var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
            //    GoogleClientSecrets.FromStream(stream).Secrets,
            //    new[] { YouTubeService.Scope.YoutubeForceSsl },
            //    "morgan.pizzini@gmail.com",
            //    CancellationToken.None
            //).Result;

            _youtubeAuthService =  new YouTubeService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "MorWalPizVideo"
            });
        }
        private async Task<IList<Google.Apis.YouTube.v3.Data.Video>> GetYouTubeVideo(IList<string> videoIds,string parameters = "snippet,contentDetails")
        {
            var request = _youtubeAuthService.Videos.List(parameters);
            request.Id = string.Join(",", videoIds);

            var response = await request.ExecuteAsync();
            return response.Items;
        }
        public async Task TranslateYoutubeVideo(IList<string> videoIds)
        {
            var targetLanguage = "en-us";
            var sourceLanguage = "it";
            var videos = await GetYouTubeVideo(videoIds, "snippet,localizations,contentDetails");
            // works only for shorts
            foreach (var video in videos.Where(video =>
                                            XmlConvert.ToTimeSpan(video.ContentDetails.Duration).TotalSeconds < 120))
            {
                if(video.Localizations != null && video.Localizations.ContainsKey(targetLanguage))
                {
                    continue;
                }
                var localizedLang = video.Localizations?[sourceLanguage];
                if(localizedLang == null)
                {
                    throw new Exception($"lang {sourceLanguage} not found for video {video.Id} [{video.Snippet.Title}]");
                }
                var stringToTranslate = $"{localizedLang.Title} | {localizedLang.Description}";
                // Traduci titolo e descrizione
                string[] translatedStrings = (await _translatorService.TranslateTextWithHashtags(stringToTranslate))
                                            .Split(" | ");
                // Aggiungi la traduzione
                AddTranslationToVideo(video, translatedStrings[0], translatedStrings[1], targetLanguage);
            }
        }

        void AddTranslationToVideo(Google.Apis.YouTube.v3.Data.Video video, string translatedTitle, string translatedDescription, string language)
        {
            // Aggiungi le traduzioni alla proprietà "Localizations"
            if (video.Localizations == null)
            {
                video.Localizations = new Dictionary<string, VideoLocalization>();
            }

            video.Localizations[language] = new VideoLocalization
            {
                Title = translatedTitle,
                Description = translatedDescription
            };

            // Aggiorna il video con le traduzioni
            var updateRequest = _youtubeAuthService.Videos.Update(video, "snippet,localizations");
            try
            {
                updateRequest.Execute();
            }
            catch(Exception ex)
            {
                throw new Exception("Errore durante l'aggiornamento del video.", ex);
            }
        }
        public async Task<IList<Video>> FetchFromYoutube(IList<string> videoIds)
        {
            var videos = new List<Video>();
            if (videoIds.Count == 0)
                return videos;

            var query = HttpUtility.ParseQueryString(string.Empty);

            query["part"] = "id,snippet,statistics,contentDetails";
            query["id"] = string.Join(",", videoIds);
            query["key"] = _apiKey;
            string queryString = query.ToString() ?? "";

            var httpResponseMessage = await _client.GetAsync($"?{queryString}");
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

        private async Task<IList<SearchResult>> FetchVideos(string channelId,int count)
        {
            var searchRequest = _youtubeAuthService.Search.List("snippet");
            searchRequest.ChannelId = channelId;
            searchRequest.Type = "video";
            searchRequest.Order = SearchResource.ListRequest.OrderEnum.Date;
            searchRequest.MaxResults = count;

            var searchResponse = await searchRequest.ExecuteAsync();
            return searchResponse.Items;
        }
        public async Task<string> GetChannelComments(string channelId, int videoCount = 10, int commentCount = 20, bool showVideo = true)
        {
            // Recupera gli ultimi video del canale
            
            var videos= await FetchVideos(channelId,100);

            var videoIds = videos.Select(item => item.Id.VideoId).ToList();

            var items = await GetYouTubeVideo(videoIds, "contentDetails");
            
            // Filtra i video normali e gli shorts
            var resultVideoIds = items.Where(video =>
                showVideo ?
                    XmlConvert.ToTimeSpan(video.ContentDetails.Duration).TotalSeconds >= 120 :
                    XmlConvert.ToTimeSpan(video.ContentDetails.Duration).TotalSeconds < 120)
                .Take(videoCount)
                .Select(x => x.Id)
                .ToList();

            var commentsBuilder = new StringBuilder();
            foreach (var searchResult in videos.Where(x =>
                                            x.Id.Kind == "youtube#video" && resultVideoIds.Contains(x.Id.VideoId)))
            {
                string videoId = searchResult.Id.VideoId;
                commentsBuilder.AppendLine($"Video: {searchResult.Snippet.Title} (ID: {videoId})");
                commentsBuilder.AppendLine("Comments:");

                // Recupera i commenti del video
                var commentRequest = _youtubeAuthService.CommentThreads.List("snippet");
                commentRequest.VideoId = videoId;
                commentRequest.MaxResults = commentCount;

                var commentResponse = await commentRequest.ExecuteAsync();
                foreach (var comment in commentResponse.Items)
                {
                    var topLevelComment = comment.Snippet.TopLevelComment;
                    var author = topLevelComment.Snippet.AuthorDisplayName;
                    var text = topLevelComment.Snippet.TextDisplay;

                    commentsBuilder.AppendLine($"- {author}: {text.ParseHTMLString()}");
                }
                commentsBuilder.AppendLine();
            }
            return commentsBuilder.ToString();
        }

        public async Task<string> GetChannelId(string channelName)
        {
            var searchRequest = _youtubeAuthService.Search.List("snippet");
            searchRequest.Q = channelName;
            searchRequest.Type = "channel";
            searchRequest.MaxResults = 1;

            var searchResponse = await searchRequest.ExecuteAsync();

            if (searchResponse.Items.Count == 0)
            {
                return string.Empty;
            }

            var channel = searchResponse.Items.First();
            return channel.Id.ChannelId;
        }

        public void Dispose()
        {
            _client?.Dispose();
            _youtubeAuthService?.Dispose();
        }
    }
}
