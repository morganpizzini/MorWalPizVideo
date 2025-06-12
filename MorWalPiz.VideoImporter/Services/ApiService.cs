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

namespace MorWalPiz.VideoImporter.Services
{
  public class ApiService
  {
    private readonly HttpClient _httpClient;

    public ApiService(IOptions<ApiSettings> options)
    {
      _httpClient = new HttpClient() {
          BaseAddress = new Uri(options.Value.ApiEndpoint)
      };
    }

    public ApiService(string apiEndpoint)
    {
      _httpClient = new HttpClient() { BaseAddress = new Uri(apiEndpoint)};
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
                if(!response.IsSuccessStatusCode)
                    return new();
        return (await response.Content.ReadFromJsonAsync<Review>())?? new();
      }
      catch (Exception ex)
      {
        return new();
      }
    }
  }

}
