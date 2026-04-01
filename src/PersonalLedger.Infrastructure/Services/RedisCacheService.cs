using Microsoft.Extensions.Caching.Distributed;
using PersonalLedger.Domain.Common;
using PersonalLedger.Domain.Common.Extensions;
using PersonalLedger.Domain.Services;
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

        public async Task<T?> ListAsync<T>(string key) where T : BaseEntity
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

       
    }
}
