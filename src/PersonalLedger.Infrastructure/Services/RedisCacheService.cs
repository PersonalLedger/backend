using Microsoft.Extensions.Caching.Distributed;
using PersonalLedger.Application.Common.Interfaces;
using PersonalLedger.Domain.Common;
using PersonalLedger.Domain.Common.Extensions;
using System.Text.Json;

namespace PersonalLedger.Infrastructure.Services
{
    public class RedisCacheService : IRedisCacheService
    {
        private readonly IDistributedCache _cache;

        public RedisCacheService(IDistributedCache cache) => _cache = cache;

        public async Task<T?> GetAsync<T>(string key) where T : BaseEntity
        {
            var data = await _cache.GetStringAsync(key);
            return data == null ? default : JsonSerializer.Deserialize<T>(data);
        }

        public async Task SetAsync<T>(T value, TimeSpan? expiration = null) where T : BaseEntity
        {
            var key = value.GenerateKey();

            if (expiration == null)
                expiration = TimeSpan.FromHours(24);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromHours(24)
            };

            var data = JsonSerializer.Serialize(value);
            await _cache.SetStringAsync(key, data, options);
        }

        public async Task RemoveAsync(string key) => await _cache.RemoveAsync(key);

        public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? expiration = null) where T : BaseEntity
        {
            var cachedValue = await GetAsync<T>(key);

            if (cachedValue is not null)
                return cachedValue;

            var value = await factory();

            if (value is not null)
                await SetAsync(value, expiration);

            return value;
        }


    }
}
