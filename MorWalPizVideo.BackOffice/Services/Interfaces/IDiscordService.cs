namespace MorWalPizVideo.BackOffice.Services.Interfaces;

public interface IDiscordService
{
    Task<string> CreatePost(string shortLink, string message);
}
