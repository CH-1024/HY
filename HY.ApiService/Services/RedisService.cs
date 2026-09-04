using StackExchange.Redis;

namespace HY.ApiService.Services
{
    public interface IRedisService
    {
        Task SetAsync(string key, string value, TimeSpan? expiry = null);
        Task<string?> GetAsync(string key);
        Task RemoveAsync(string key);
        Task<bool> SetAddAsync(string key, string value);
        Task<bool> SetRemoveAsync(string key, string value);
        Task<string[]> SetMembersAsync(string key);
    }


    public class RedisService : IRedisService
    {
        private readonly IConnectionMultiplexer _redis;

        public RedisService(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        private IDatabase Db => _redis.GetDatabase();

        public Task SetAsync(string key, string value, TimeSpan? expiry = null)
        {
            return Db.StringSetAsync(key, value, expiry, When.Always);
        }

        public async Task<string?> GetAsync(string key)
        {
            var value = await Db.StringGetAsync(key);

            return value.HasValue ? value.ToString() : null;
        }

        public Task RemoveAsync(string key)
        {
            return Db.KeyDeleteAsync(key);
        }

        public Task<bool> SetAddAsync(string key, string value)
        {
            return Db.SetAddAsync(key, value);
        }

        public Task<bool> SetRemoveAsync(string key, string value)
        {
            return Db.SetRemoveAsync(key, value);
        }

        public async Task<string[]> SetMembersAsync(string key)
        {
            var values = await Db.SetMembersAsync(key);

            return values.Select(x => x.ToString()).ToArray();
        }

    }
}
