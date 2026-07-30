using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using MorWalPiz.Contracts.DTOs;
using MorWalPiz.InsightScanner.Models;

namespace MorWalPiz.InsightScanner.Services
{
    /// <summary>
    /// API-key authenticated HTTP client for the BackOffice Insights endpoints, following the
    /// same HttpClient-per-instance convention used by MorWalPiz.VideoImporter's ApiService.
    /// </summary>
    public class BackOfficeInsightClient : IBackOfficeInsightClient
    {
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public BackOfficeInsightClient(string apiEndpoint, string? apiKey)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(apiEndpoint),
                Timeout = TimeSpan.FromSeconds(100)
            };

            if (!string.IsNullOrEmpty(apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
            }
        }

        public async Task<List<InsightTopicSummary>> GetTopicsAsync()
        {
            var response = await _httpClient.GetAsync("api/insights/topics");
            response.EnsureSuccessStatusCode();

            var topics = await response.Content.ReadFromJsonAsync<List<InsightTopicSummary>>(JsonOptions);
            return topics ?? [];
        }

        public async Task<ManualScanResponseDto> SubmitManualScanAsync(string topicId, ManualScanRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync($"api/insights/topics/{topicId}/manual-scan", request, JsonOptions);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Manual scan request failed: {response.StatusCode} - {body}");
            }

            var result = await response.Content.ReadFromJsonAsync<ManualScanResponseDto>(JsonOptions);
            return result ?? new ManualScanResponseDto();
        }
    }
}
