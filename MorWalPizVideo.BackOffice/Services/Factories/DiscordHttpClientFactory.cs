using System.Net.Http.Headers;
using MorWalPizVideo.BackOffice.Services.Configuration;

namespace MorWalPizVideo.BackOffice.Services.Factories
{
    public class DiscordHttpClientFactory : IDiscordHttpClientFactory
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IDiscordConfigurationService _configService;

        public DiscordHttpClientFactory(
            IHttpClientFactory httpClientFactory,
            IDiscordConfigurationService configService)
        {
            _httpClientFactory = httpClientFactory;
            _configService = configService;
        }

        public HttpClient CreateClient()
        {
            var client = _httpClientFactory.CreateClient();
            var settings = _configService.GetDiscordSettings();
            
            client.BaseAddress = new Uri("https://discord.com/api/");
            client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bot", settings.Token);
            
            return client;
        }
    }
}
