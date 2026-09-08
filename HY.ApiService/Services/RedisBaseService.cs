using StackExchange.Redis;

namespace HY.ApiService.Services
{
    public interface IRedisBaseService
    {
        ITransaction CreateTransaction();

        Task<string> HashGetAsync(string key, RedisValue redisValue);
        Task<string[]> HashGetAsync(string key, RedisValue[] redisValues);
        Task<bool> HashSetAsync(string key, HashEntry[] hashEntries);
        Task<bool> HashSetAsync(string key, HashEntry hashEntry);

        Task<string?> StringGetAsync(string key);
        Task<bool> StringSetAsync(string key, string value, TimeSpan? expiry = null, When when = When.Always);

        Task<bool> KeyDeleteAsync(string key);
        Task<bool> KeyExpireAsync(string key, TimeSpan expiry);
        Task<bool> KeyExpireAsync(string key, DateTime expiry);
        Task<bool> KeyPersistAsync(string key);

        Task<bool> SetAddAsync(string key, string value);
        Task<bool> SetRemoveAsync(string key, string value);
        Task<string[]> SetMembersAsync(string key);
    }


    public class RedisBaseService : IRedisBaseService
    {
        private readonly IConnectionMultiplexer _redis;

        public RedisBaseService(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        private IDatabase Db => _redis.GetDatabase();



        public ITransaction CreateTransaction()
        {
            return Db.CreateTransaction();
        }



        public async Task<string> HashGetAsync(string key, RedisValue redisValue)
        {
            var value = await Db.HashGetAsync(key, redisValue);
            return value.ToString();
        }

        public async Task<string[]> HashGetAsync(string key, RedisValue[] redisValues)
        {
            var values = await Db.HashGetAsync(key, redisValues);
            return values?.Select(x => x.ToString()).ToArray() ?? [];
        }

        public async Task<bool> HashSetAsync(string key, HashEntry[] hashEntries)
        {
            await Db.HashSetAsync(key, hashEntries);
            return true;
        }

        public async Task<bool> HashSetAsync(string key, HashEntry hashEntry)
        {
            await Db.HashSetAsync(key, hashEntry.Name, hashEntry.Value);
            return true;
        }



        public async Task<string?> StringGetAsync(string key)
        {
            var value = await Db.StringGetAsync(key);
            return value.HasValue ? value.ToString() : null;
        }

        public async Task<bool> StringSetAsync(string key, string value, TimeSpan? expiry = null, When when = When.Always)
        {
            return await Db.StringSetAsync(key, value, expiry, when);
        }



        public async Task<bool> KeyDeleteAsync(string key)
        {
            return await Db.KeyDeleteAsync(key);
        }

        public async Task<bool> KeyExpireAsync(string key, TimeSpan expiry)
        {
            return await Db.KeyExpireAsync(key, expiry);
        }

        public async Task<bool> KeyExpireAsync(string key, DateTime expiry)
        {
            return await Db.KeyExpireAsync(key, expiry);
        }

        public async Task<bool> KeyPersistAsync(string key)
        {
            return await Db.KeyPersistAsync(key);
        }


        public async Task<bool> SetAddAsync(string key, string value)
        {
            return await Db.SetAddAsync(key, value);
        }

        public async Task<bool> SetRemoveAsync(string key, string value)
        {
            return await Db.SetRemoveAsync(key, value);
        }

        public async Task<string[]> SetMembersAsync(string key)
        {
            var values = await Db.SetMembersAsync(key);
            return values?.Select(x => x.ToString()).ToArray() ?? [];
        }

    }
}
