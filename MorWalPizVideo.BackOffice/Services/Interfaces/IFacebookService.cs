namespace MorWalPizVideo.BackOffice.Services.Interfaces;

public interface IFacebookService
{
    Task<string> CreatePost(string shortLink, string message);
}