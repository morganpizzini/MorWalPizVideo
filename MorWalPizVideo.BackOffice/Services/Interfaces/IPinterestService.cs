namespace MorWalPizVideo.BackOffice.Services.Interfaces;

public interface IPinterestService
{
    Task<string> ExchangeCodeForTokenAsync(string code, string redirectUri);
    Task<string> CreatePinAsync(string accessToken, string boardId, string link, string title, string description, string imageUrl);
}
