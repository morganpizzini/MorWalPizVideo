using MorWalPizVideo.BackOffice;

namespace MorWalPizVideo.BackOffice.Services.Configuration
{
    public interface ITelegramConfigurationService
    {
        TelegramSettings GetTelegramSettings();
    }
}
