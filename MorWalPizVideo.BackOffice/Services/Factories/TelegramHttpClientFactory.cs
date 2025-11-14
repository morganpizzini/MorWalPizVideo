using System.Net.Http.Headers;
using MorWalPizVideo.BackOffice.Services.Configuration;

namespace MorWalPizVideo.BackOffice.Services.Factories
{
    public class TelegramHttpClientFactory : ITelegramHttpClientFactory
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITelegramConfigurationService _configService;

        public TelegramHttpClientFactory(
            IHttpClientFactory httpClientFactory,
            ITelegramConfigurationService configService)
        {
            _httpClientFactory = httpClientFactory;
            _configService = configService;
        }

        public HttpClient CreateClient()
        {
            var client = _httpClientFactory.CreateClient();
            var settings = _configService.GetTelegramSettings();
            
            client.BaseAddress = new Uri($"https://api.telegram.org/bot{settings.Token}/sendMessage");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            
            return client;
        }
    }
}
