namespace MorWalPizVideo.BackOffice.Services.Interfaces;

public interface ITelegramService
{
    Task<string> CreatePost(string shortLink, string message);
}
