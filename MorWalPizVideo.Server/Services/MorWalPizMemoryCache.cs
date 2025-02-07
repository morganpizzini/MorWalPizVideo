using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace MorWalPizVideo.Server.Services
{

    public interface IMorWalPizCache
    {
        T? Get<T>(string key);
        Task<T?> GetAsync<T>(string key);
        void Set<T>(string key, T value, DistributedCacheEntryOptions options);
        Task SetAsync<T>(string key, T value, DistributedCacheEntryOptions options);
        void Refresh(string key);
        Task RefreshAsync(string key);
        void Remove(string key);
        Task RemoveAsync(string key);
    }
    public class MorWalPizMemoryCache : IMorWalPizCache
    {
        private readonly IDistributedCache _cache;
        public MorWalPizMemoryCache(IDistributedCache cache)
        {
            _cache = cache;
        }

        public T? Get<T>(string key)
        {
            return ConvertObjectFromByteArray<T>(_cache.Get(key));
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            return ConvertObjectFromByteArray<T>(await _cache.GetAsync(key));
        }

        private T? ConvertObjectFromByteArray<T>(byte[]? array)
        {
            if (array == null)
                return default;

            var jsonToDeserialize = System.Text.Encoding.UTF8.GetString(array);
            var cachedResult = JsonSerializer.Deserialize<T>(jsonToDeserialize);

            return cachedResult == null ? default : cachedResult;
        }

        public void Refresh(string key)
        {
            _cache.Refresh(key);
        }

        public Task RefreshAsync(string key)
        {
            return _cache.RefreshAsync(key);
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
        }

        public Task RemoveAsync(string key)
        {
            return _cache.RemoveAsync(key);
        }

        public void Set<T>(string key, T value, DistributedCacheEntryOptions options)
        {
            byte[] objectToCache = value is byte[] v ? v : JsonSerializer.SerializeToUtf8Bytes(value);
            _cache.Set(key, objectToCache, options);
        }

        public Task SetAsync<T>(string key, T value, DistributedCacheEntryOptions options)
        {
            byte[] objectToCache = value is byte[] v ? v : JsonSerializer.SerializeToUtf8Bytes(value);
            return _cache.SetAsync(key, objectToCache, options);
        }
    }

    public class MorWalPizMemoryCacheMock : IMorWalPizCache
    {
        public T? Get<T>(string key)
        {
            return default;
        }

        public Task<T?> GetAsync<T>(string key)
        {
            return Task.FromResult<T?>(default);
        }

        public void Refresh(string key)
        {

        }

        public Task RefreshAsync(string key)
        {
            return Task.CompletedTask;
        }

        public void Remove(string key)
        {
        }

        public Task RemoveAsync(string key)
        {
            return Task.CompletedTask;
        }

        public void Set<T>(string key, T value, DistributedCacheEntryOptions options)
        {

        }

        public Task SetAsync<T>(string key, T value, DistributedCacheEntryOptions options)
        {
            return Task.CompletedTask;
        }
    }
    public static class RmdCacheExtensions
    {
        public static void Set<T>(this IMorWalPizCache cache, string key, T value, int slidingExpirationMin = 30, int expirationHours = 1)
        {
            var cacheEntryOptions = new DistributedCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(slidingExpirationMin))
                .SetAbsoluteExpiration(TimeSpan.FromHours(expirationHours));

            cache.Set(key, value, cacheEntryOptions);
        }

        public static bool TryGetValue<T>(this IMorWalPizCache cache, string key, out T value)
        {
            var res = cache.Get<T>(key);
            value = res == null ? default! : res;
            return res != null;
        }

        public static TItem GetOrCreate<TItem>(this IMorWalPizCache cache, string key, Func<TItem> factory, int slidingExpirationMin = 30, int expirationHours = 1)
        {
            if (!cache.TryGetValue(key, out TItem result))
            {
                result = factory();
                cache.Set(key, result, slidingExpirationMin, expirationHours);
            }

            return result;
        }

        public static async Task<TItem> GetOrCreateAsync<TItem>(this IMorWalPizCache cache, string key, Func<Task<TItem>> factory, int slidingExpirationMin = 30, int expirationHours = 1)
        {
            if (!cache.TryGetValue(key, out TItem result))
            {
                result = await factory();
                cache.Set(key, result, slidingExpirationMin, expirationHours);
            }

            return result;
        }
    }
}
