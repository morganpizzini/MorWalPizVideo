using System.Net.Http;
using System.Net.Http.Json;
using MorWalPiz.VideoImporter.Models;
using MorWalPizVideo.BackOffice.DTOs;
using BackOfficeDTOs = MorWalPizVideo.BackOffice.DTOs;
using MorWalPiz.Contracts.DTOs;

namespace MorWalPiz.VideoImporter.Services
{
    public interface IApiServiceFactory
    {
        ApiService Create(string apiEndpoint, string? apiKey = null, string? channelId = null);
    }

    public sealed class ApiServiceFactory : IApiServiceFactory
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ApiServiceFactory(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public ApiService Create(string apiEndpoint, string? apiKey = null, string? channelId = null)
        {
            var httpClient = _httpClientFactory.CreateClient("BackOffice");
            httpClient.BaseAddress = new Uri(apiEndpoint);
            if (!string.IsNullOrEmpty(apiKey))
            {
                httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
            }
            if (!string.IsNullOrWhiteSpace(channelId))
            {
                httpClient.DefaultRequestHeaders.Add("X-Channel-Id", channelId);
            }

            return new ApiService(httpClient);
        }
    }

    public class ApiService
    {
        private readonly HttpClient _httpClient;

        public ApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IList<ReviewApiVideoResponse>> SendVideosContextAsync(IEnumerable<string> videoNames, string context, IList<Language> languagues)
        {
            try
            {
                var requestData = new ReviewRequest
                {
                    Names = [.. videoNames],
                    Context = context,
                    Languages = languagues.Select(l => l.Name).ToList()
                };

                var response = await _httpClient.PostAsJsonAsync("api/chat", requestData);
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Error: {response.StatusCode}, {await response.Content.ReadAsStringAsync()}");
                }
                return (await response.Content.ReadFromJsonAsync<IList<ReviewApiVideoResponse>>()) ?? [];
            }
            catch
            {
                return [];
            }
        }

        public async Task<List<BackOfficeDTOs.VideoTranslationResponse>> TranslateVideoContentAsync(string title, string description, IList<Language> languages)
        {
            try
            {
                var requestData = new BackOfficeDTOs.VideoTranslationRequest
                {
                    Title = title,
                    Description = description,
                    Languages = languages.Select(l => l.Code).ToList()
                };

                var response = await _httpClient.PostAsJsonAsync("api/chat/translate", requestData);
                if (!response.IsSuccessStatusCode)
                    return new List<BackOfficeDTOs.VideoTranslationResponse>();
                return (await response.Content.ReadFromJsonAsync<List<BackOfficeDTOs.VideoTranslationResponse>>()) ?? new List<BackOfficeDTOs.VideoTranslationResponse>();
            }
            catch (Exception)
            {
                return new List<BackOfficeDTOs.VideoTranslationResponse>();
            }
        }

        public async Task<TranscriptAnalysisResponse> AnalyzeTranscriptAsync(TranscriptAnalysisRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/chat/transcript-analysis", request);
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Error: {response.StatusCode}, {await response.Content.ReadAsStringAsync()}");
                }
                return (await response.Content.ReadFromJsonAsync<TranscriptAnalysisResponse>()) ?? new TranscriptAnalysisResponse();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to analyze transcript: {ex.Message}", ex);
            }
        }

    }

}
