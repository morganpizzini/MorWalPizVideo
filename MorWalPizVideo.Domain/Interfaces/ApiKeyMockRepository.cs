using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.Domain;

public class ApiKeyMockRepository : BaseMockRepository<ApiKey>, IApiKeyRepository
{
    public ApiKeyMockRepository(IMockScenario scenario) : base(scenario, "apiKeys")
    {
    }

    public async Task<ApiKey?> GetByKeyAsync(string key)
    {
        var items = await GetItemsAsync();
        return items.FirstOrDefault(x => x.Key == key);
    }

    public async Task<ApiKey?> GetByNameAsync(string name)
    {
        var items = await GetItemsAsync();
        return items.FirstOrDefault(x => x.Name == name);
    }

    public async Task<IEnumerable<ApiKey>> GetActiveKeysAsync()
    {
        var items = await GetItemsAsync();
        return items.Where(x => x.IsActive);
    }
}

public sealed class SocialAssetMockRepository(IMockScenario scenario)
    : BaseMockRepository<SocialAsset>(scenario, "socialAssets"), ISocialAssetRepository
{
    public async Task<SocialAsset?> GetByIdempotencyKeyAsync(string channelId, string idempotencyKey) =>
        (await GetItemsAsync(x => x.ChannelId == channelId && x.IdempotencyKey == idempotencyKey)).FirstOrDefault();
}
