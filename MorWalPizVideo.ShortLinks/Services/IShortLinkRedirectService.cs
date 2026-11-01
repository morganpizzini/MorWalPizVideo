using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.ShortLinks.Services;

public interface IShortLinkRedirectService
{
    Task<ShortLink?> GetByCodeAsync(string code);
    Task<int> IncrementClicksAsync(string id);
    Task<int> IncrementClicksAsync(string id, ShortLinkRequestCategory category, DateTime timestamp);
}

public sealed class ShortLinkRedirectService(IShortLinkRepository repository) : IShortLinkRedirectService
{
    public Task<ShortLink?> GetByCodeAsync(string code) => repository.GetByCodeAsync(code);

    public Task<int> IncrementClicksAsync(string id) => repository.IncrementClicksAsync(id);

    public Task<int> IncrementClicksAsync(string id, ShortLinkRequestCategory category, DateTime timestamp)
        => repository.IncrementClicksAsync(id, category, timestamp);
}