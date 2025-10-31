using MorWalPizVideo.BackOffice;

namespace MorWalPizVideo.BackOffice.Services.Configuration
{
    public interface IDiscordConfigurationService
    {
        TelegramSettings GetDiscordSettings();
    }
}
