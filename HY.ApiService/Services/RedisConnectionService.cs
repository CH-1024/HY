using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Runtime.InteropServices;

namespace HY.ApiService.Services
{
    public interface IRedisConnectionService
    {
        Task<string?> GetConnectionIdAsync(long userId, int platform);
        Task SetConnectionAsync(long userId, int platform, string connectionId);
        Task RemoveConnectionAsync(long userId, int platform, string connectionId);
        Task<List<string>> GetAllPlatformConnectionIdsAsync(long userId);
        Task<List<string>> GetOtherPlatformConnectionIdsAsync(long userId, int platform);
        Task<bool> IsOnlineAsync(long userId);
        Task<bool> IsOnlineAsync(long userId, int platform);
    }


    public class RedisConnectionService : IRedisConnectionService
    {
        private readonly IRedisService _redis;

        public RedisConnectionService(IRedisService redis)
        {
            _redis = redis;
        }

        private static string ConnectionKey(long userId, int platform)
        {
            return $"SignalR:Connection:{userId}:{platform}";
        }

        private static string PlatformsKey(long userId)
        {
            return $"SignalR:Connections:{userId}";
        }



        public async Task<string?> GetConnectionIdAsync(long userId, int platform)
        {
            var ckey = ConnectionKey(userId, platform);

            return await _redis.GetAsync(ckey);
        }

        public async Task SetConnectionAsync(long userId, int platform, string connectionId)
        {
            var ckey = ConnectionKey(userId, platform);
            var pkey = PlatformsKey(userId);

            await _redis.SetAsync(ckey, connectionId);
            await _redis.SetAddAsync(pkey, platform.ToString());
        }

        public async Task RemoveConnectionAsync(long userId, int platform, string connectionId)
        {
            var ckey = ConnectionKey(userId, platform);
            var pkey = PlatformsKey(userId);

            var currentConnection = await _redis.GetAsync(ckey);

            // 防止旧连接把新连接删除
            if (currentConnection != connectionId)
                return;

            await _redis.RemoveAsync(ckey);
            await _redis.SetRemoveAsync(pkey, platform.ToString());
        }

        public async Task<List<string>> GetAllPlatformConnectionIdsAsync(long userId)
        {
            var pkey = PlatformsKey(userId);

            var platforms = await _redis.SetMembersAsync(pkey);

            var result = new List<string>();

            foreach (var platform in platforms)
            {
                var ckey = ConnectionKey(userId, int.Parse(platform));

                var connectionId = await _redis.GetAsync(ckey);

                if (!string.IsNullOrEmpty(connectionId))
                    result.Add(connectionId);
            }

            return result;
        }

        public async Task<List<string>> GetOtherPlatformConnectionIdsAsync(long userId, int platform)
        {
            var pkey = PlatformsKey(userId);

            var platforms = await _redis.SetMembersAsync(pkey);

            var result = new List<string>();

            foreach (var item in platforms)
            {
                if (int.Parse(item) == platform)
                    continue;

                var ckey = ConnectionKey(userId, int.Parse(item));

                var connectionId = await _redis.GetAsync(ckey);

                if (!string.IsNullOrEmpty(connectionId))
                    result.Add(connectionId);
            }

            return result;
        }

        public async Task<bool> IsOnlineAsync(long userId)
        {
            var connectionIds = await GetAllPlatformConnectionIdsAsync(userId);

            return connectionIds.Count > 0;
        }

        public async Task<bool> IsOnlineAsync(long userId, int platform)
        {
            var connectionId = await GetConnectionIdAsync(userId, platform);

            return !string.IsNullOrEmpty(connectionId);
        }
    }
}
