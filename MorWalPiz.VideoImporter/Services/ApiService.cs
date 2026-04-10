using System.Net.Http;
using System.Net.Http.Json;
using MorWalPiz.VideoImporter.Models;
using MorWalPizVideo.BackOffice.DTOs;
using BackOfficeDTOs = MorWalPizVideo.BackOffice.DTOs;
using MorWalPiz.Contracts.DTOs;

namespace MorWalPiz.VideoImporter.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string? _apiKey;

        public ApiService(string apiEndpoint, string? apiKey = null)
        {
            var handler = new SocketsHttpHandler
            {
                ConnectTimeout = TimeSpan.FromSeconds(30),            // TCP handshake timeout
                PooledConnectionLifetime = TimeSpan.FromMinutes(10),  // Optional, for reusing connections
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(20)       // Optional, for long-lived idle connections
            };
            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(apiEndpoint),
                Timeout = TimeSpan.FromSeconds(300) // 5 minutes timeout
            };
            
            _apiKey = apiKey;
            
            // Add API Key to default headers if provided
            if (!string.IsNullOrEmpty(_apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("X-API-Key", _apiKey);
            }
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
