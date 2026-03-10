using Microsoft.Extensions.Hosting;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.Domain;

public class ApiKeyMockRepository : BaseMockRepository<ApiKey>, IApiKeyRepository
{
    public ApiKeyMockRepository(IHostEnvironment environment) : base(environment, "apiKeys")
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
