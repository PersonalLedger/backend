using PersonalLedger.Domain.Common;

namespace PersonalLedger.Domain.Services
{
    public interface IRedisCacheService
    {
        Task<T?> GetAsync<T>(string key) where T : BaseEntity;
        Task SetAsync<T>(T value, TimeSpan? expiration = null) where T : BaseEntity;
        Task RemoveAsync(string key);
    }
}
