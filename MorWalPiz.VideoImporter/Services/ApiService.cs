using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MorWalPiz.VideoImporter.Models;
using MorWalPizVideo.BackOffice.DTOs;
using BackOfficeDTOs = MorWalPizVideo.BackOffice.DTOs;

namespace MorWalPiz.VideoImporter.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;


        public ApiService(string apiEndpoint)
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
        }

        public async Task<Review> SendVideosContextAsync(IEnumerable<string> videoNames, string context, IList<Language> languagues)
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
                return (await response.Content.ReadFromJsonAsync<Review>()) ?? new();
            }
            catch
            {
                throw;
            }
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
        if(!response.IsSuccessStatusCode)
            return new List<BackOfficeDTOs.VideoTranslationResponse>();
        return (await response.Content.ReadFromJsonAsync<List<BackOfficeDTOs.VideoTranslationResponse>>()) ?? new List<BackOfficeDTOs.VideoTranslationResponse>();
      }
      catch (Exception)
      {
        return new List<BackOfficeDTOs.VideoTranslationResponse>();
      }
    }
  }

}
