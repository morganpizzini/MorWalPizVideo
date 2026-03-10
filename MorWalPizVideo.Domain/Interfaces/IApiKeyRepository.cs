using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.Domain.Interfaces;

public interface IApiKeyRepository : IRepository<ApiKey>
{
    Task<ApiKey?> GetByKeyAsync(string key);
    Task<ApiKey?> GetByNameAsync(string name);
    Task<IEnumerable<ApiKey>> GetActiveKeysAsync();
}
