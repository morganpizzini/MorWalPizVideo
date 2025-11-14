using Microsoft.Extensions.Options;

namespace MorWalPizVideo.BackOffice.Services.Configuration
{
    public class DiscordConfigurationService : IDiscordConfigurationService
    {
        private readonly Lazy<TelegramSettings> _settings;

        public DiscordConfigurationService(IOptionsMonitor<TelegramSettings> options)
        {
            _settings = new Lazy<TelegramSettings>(() =>
            {
                var settings = options.Get("DiscordSettings");
                if (string.IsNullOrEmpty(settings.Token))
                {
                    throw new InvalidOperationException("Discord configuration is not properly set. Check your configuration sources including Azure Key Vault.");
                }
                return settings;
            });
        }

        public TelegramSettings GetDiscordSettings() => _settings.Value;
    }
}
