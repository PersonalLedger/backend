using PersonalLedger.Domain.Common;

namespace PersonalLedger.Application.Common.Interfaces
{
    public interface IRedisCacheService
    {
        Task<T?> GetAsync<T>(string key) where T : BaseEntity;
        Task SetAsync<T>(T value, TimeSpan? expiration = null) where T : BaseEntity;
        Task RemoveAsync(string key);
        Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? expiration = null) where T : BaseEntity;
    }
}
