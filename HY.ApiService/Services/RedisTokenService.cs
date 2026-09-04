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
        private readonly IRedisService _redis;

        public RedisTokenService(IRedisService redis)
        {
            _redis = redis;
        }

        private static string TokenKey(long userId, string deviceId)
        {
            return $"Auth:Token:{userId}:{deviceId}";
        }



        public async Task SaveAsync(long userId, string deviceId, string accessToken, DateTime expires)
        {
            var key = TokenKey(userId, deviceId);
            var expiry = expires - DateTime.UtcNow;

            await _redis.SetAsync(key, accessToken, expiry);
        }

        public async Task<bool> ExistsAsync(long userId, string deviceId, string accessToken)
        {
            var key = TokenKey(userId, deviceId);

            var token = await _redis.GetAsync(key);

            return token == accessToken;
        }

        public async Task RemoveAsync(long userId, string deviceId)
        {
            var key = TokenKey(userId, deviceId);

            await _redis.RemoveAsync(key);
        }
    }
}
