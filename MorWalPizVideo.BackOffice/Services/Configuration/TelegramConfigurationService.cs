using Microsoft.Extensions.Options;

namespace MorWalPizVideo.BackOffice.Services.Configuration
{
    public class TelegramConfigurationService : ITelegramConfigurationService
    {
        private readonly Lazy<TelegramSettings> _settings;

        public TelegramConfigurationService(IOptionsMonitor<TelegramSettings> options)
        {
            _settings = new Lazy<TelegramSettings>(() =>
            {
                var settings = options.Get("TelegramSettings");
                if (string.IsNullOrEmpty(settings.Token))
                {
                    throw new InvalidOperationException("Telegram configuration is not properly set. Check your configuration sources including Azure Key Vault.");
                }
                return settings;
            });
        }

        public TelegramSettings GetTelegramSettings() => _settings.Value;
    }
}
