using Microsoft.Extensions.Caching.Distributed;
using NetTaste;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace HY.ApiService.Services
{
    public interface IRedisTokenService
    {
        Task SaveAsync(long userId, string deviceId, string accessToken, DateTime expires);
        Task<bool> ExistsAsync(long userId, string deviceId, string accessToken);
        Task RemoveAsync(long userId, string deviceId);
    }


    public class RedisTokenService : IRedisTokenService
    {
        private readonly IDistributedCache _cache;

        public RedisTokenService(IDistributedCache cache)
        {
            _cache = cache;
        }


        private string Key(long userId, string deviceId) => $"user:tokens:{userId}:{deviceId}";

        public async Task SaveAsync(long userId, string deviceId, string accessToken, DateTime expires)
        {
            var key = Key(userId, deviceId);
            var token = accessToken;
            var expiry = expires - DateTime.UtcNow;

            await _cache.SetStringAsync(key, token, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry // 设置过期时间
            });
        }

        public async Task<bool> ExistsAsync(long userId, string deviceId, string accessToken)
        {
            var key = Key(userId, deviceId);
            var token = await _cache.GetStringAsync(key);

            return token == accessToken;
        }

        public async Task RemoveAsync(long userId, string deviceId)
        {
            var key = Key(userId, deviceId);
            await _cache.RemoveAsync(key);
        }
    }
}
