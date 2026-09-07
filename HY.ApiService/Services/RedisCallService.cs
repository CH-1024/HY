using HY.ApiService.Dtos;
using HY.ApiService.Enums;
using StackExchange.Redis;

namespace HY.ApiService.Services
{
    public interface IRedisCallService
    {
        Task<bool> IsUserCalling(long userId);
        Task<bool> IsUserPlatformCalling(long userId, int platform);
        Task<string?> GetCallId(long userId);
        Task<CallDto?> GetCall(string callId);

        Task<bool> CreateCall(CallDto callDto);
        Task<bool> CancelCall(string callId, long callerId, int callerPlatform);
        Task<bool> AcceptCall(string callId, long calleeId, int calleePlatform);
        Task<bool> RejectCall(string callId, long calleeId, int calleePlatform);
        Task<bool> HangUpCall(string callId, long userId);
        Task<bool> AbnormalCall(string callId);

        Task<bool> EndCall(string callId);
    }


    public class RedisCallService : IRedisCallService
    {
        private readonly IRedisBaseService _redis;


        public RedisCallService(IRedisBaseService redis)
        {
            _redis = redis;
        }


        private static string CallKey(string callId)
        {
            return $"Call:{callId}";
        }

        private static string UserKey(long userId)
        {
            return $"User:{userId}:Call";
        }



        public async Task<bool> IsUserCalling(long userId)
        {
            var userKey = UserKey(userId);

            var callId = await _redis.StringGetAsync(userKey);
            if (string.IsNullOrEmpty(callId))
            {
                return false;
            }

            var callKey = CallKey(callId);

            var callStateStr = await _redis.HashGetAsync(callKey, new RedisValue("CallState"));
            if (!Enum.TryParse<CallState>(callStateStr, out var callState))
                throw new Exception($"CallState Error : {callStateStr}");

            return callState == CallState.Calling &&
                   callState == CallState.Accepted &&
                   callState == CallState.Connected;
        }

        public async Task<bool> IsUserPlatformCalling(long userId, int platform)
        {
            var userKey = UserKey(userId);

            var callId = await _redis.StringGetAsync(userKey);
            if (string.IsNullOrEmpty(callId))
            {
                return false;
            }

            var callDto = await GetCall(callId);
            if (callDto!.CallerId == userId && callDto!.CallerPlatform == platform)
            {
                return callDto.CallState == CallState.Calling &&
                       callDto.CallState == CallState.Accepted &&
                       callDto.CallState == CallState.Connected;
            }
            else if (callDto!.CalleeId == userId && callDto!.CalleePlatform == platform)
            {
                return callDto.CallState == CallState.Calling &&
                       callDto.CallState == CallState.Accepted &&
                       callDto.CallState == CallState.Connected;
            }
            else
            {
                return false;
            }
        }

        public async Task<string?> GetCallId(long userId)
        {
            var userKey = UserKey(userId);

            return await _redis.StringGetAsync(userKey);
        }

        public async Task<CallDto?> GetCall(string callId)
        {
            var callKey = CallKey(callId);

            var redisValues = new RedisValue[]
            {
                "CallType",
                "ChatType",
                "CallerId",
                "CallerPlatform",
                "CalleeId",
                "CalleePlatform",
                "CallState",
                "CreateAt",
                "ExpiresAt"
            };
            var values = await _redis.HashGetAsync(callKey, redisValues);
            if (values.Length == 0)
            {
                return null;
            }

            return new CallDto
            {
                CallId = callId,
                CallType = Enum.Parse<CallType>(values[0]),
                ChatType = Enum.Parse<ChatType>(values[1]),
                CallerId = long.Parse(values[2]),
                CallerPlatform = int.Parse(values[3]),
                CalleeId = long.Parse(values[4]),
                CalleePlatform = int.Parse(values[5]),
                CallState = Enum.Parse<CallState>(values[6]),
                CreateAt = DateTime.Parse(values[7]),
                ExpiryAt = DateTime.Parse(values[8]),
            };
        }


        public async Task<bool> CreateCall(CallDto callDto)
        {
            var callKey = CallKey(callDto.CallId);
            var callerKey = UserKey(callDto.CallerId);
            var calleeKey = UserKey(callDto.CalleeId);

            var entries = new HashEntry[]
            {
                new("CallType", callDto.CallType.ToString()),
                new("ChatType", callDto.ChatType.ToString()),
                new("CallerId", callDto.CallerId),
                new("CallerPlatform", callDto.CallerPlatform),
                new("CalleeId", callDto.CalleeId),
                new("CalleePlatform", callDto.CalleePlatform),
                new("CallState", callDto.CallState.ToString()),
                new("CreateAt", callDto.CreateAt.ToString("O")),
                new("ExpiresAt", callDto.ExpiryAt.ToString("O"))
            };

            var transaction = _redis.CreateTransaction();

            // 双方都必须没有正在进行的通话
            transaction.AddCondition(Condition.KeyNotExists(callerKey));
            transaction.AddCondition(Condition.KeyNotExists(calleeKey));

            // 创建通话
            transaction.HashSetAsync(callKey, entries);

            // 建立用户 → CallId 索引
            transaction.StringSetAsync(callerKey, callDto.CallId);
            transaction.StringSetAsync(calleeKey, callDto.CallId);

            // 设置过期时间
            transaction.KeyExpireAsync(callKey, callDto.ExpiryAt);
            transaction.KeyExpireAsync(callerKey, callDto.ExpiryAt);
            transaction.KeyExpireAsync(calleeKey, callDto.ExpiryAt);

            return await transaction.ExecuteAsync();
        }

        public async Task<bool> CancelCall(string callId, long callerId, int callerPlatform)
        {
            var callKey = CallKey(callId);
            var callerKey = UserKey(callerId);

            var entries = new HashEntry[]
            {
                new("CallState", CallState.Cancelled.ToString())
            };

            // callId 必须与 callerKey 中的值匹配
            var _callId = await _redis.StringGetAsync(callerKey);
            if (string.IsNullOrEmpty(_callId) || _callId != callId)
                return false;

            // callKey 中的 CallerId 必须与 callerId 匹配
            var _callerId = await _redis.HashGetAsync(callKey, new RedisValue("CallerId"));
            if (!long.TryParse(_callerId, out var parsedCallerId) || parsedCallerId != callerId)
                return false;

            await _redis.HashSetAsync(callKey, entries);

            return true;
        }

        public async Task<bool> AcceptCall(string callId, long calleeId, int calleePlatform)
        {
            var callKey = CallKey(callId);
            var calleeKey = UserKey(calleeId);

            var entries = new HashEntry[]
            {
                new("CalleePlatform", calleePlatform),
                new("CallState", CallState.Accepted.ToString())
            };

            // callId 必须与 calleeKey 中的值匹配
            var _callId = await _redis.StringGetAsync(calleeKey);
            if (string.IsNullOrEmpty(_callId) || _callId != callId)
                return false;

            // callKey 中的 CalleeId 必须与 calleeId 匹配
            var _calleeId = await _redis.HashGetAsync(callKey, new RedisValue("CalleeId"));
            if (!long.TryParse(_calleeId, out var parsedCalleeId) || parsedCalleeId != calleeId)
                return false;

            await _redis.HashSetAsync(callKey, entries);

            return true;
        }

        public async Task<bool> RejectCall(string callId, long calleeId, int calleePlatform)
        {
            var callKey = CallKey(callId);
            var calleeKey = UserKey(calleeId);

            var entries = new HashEntry[]
            {
                new("CalleePlatform", calleePlatform),
                new("CallState", CallState.Rejected.ToString())
            };

            // callId 必须与 calleeKey 中的值匹配
            var _callId = await _redis.StringGetAsync(calleeKey);
            if (string.IsNullOrEmpty(_callId) || _callId != callId)
                return false;

            // callKey 中的 CalleeId 必须与 calleeId 匹配
            var _calleeId = await _redis.HashGetAsync(callKey, new RedisValue("CalleeId"));
            if (!long.TryParse(_calleeId, out var parsedCalleeId) || parsedCalleeId != calleeId)
                return false;

            await _redis.HashSetAsync(callKey, entries);

            return true;
        }

        public async Task<bool> HangUpCall(string callId, long userId)
        {
            var callKey = CallKey(callId);
            var userKey = UserKey(userId);

            // callId 必须与 userKey 中的值匹配
            var _callId = await _redis.StringGetAsync(userKey);
            if (string.IsNullOrEmpty(_callId) || _callId != callId)
                return false;

            await _redis.HashSetAsync(callKey, new HashEntry("CallState", CallState.Ended.ToString()));

            return true;
        }

        // 仅 OnDisconnectedAsync 调用
        public async Task<bool> AbnormalCall(string callId)
        {
            var callKey = CallKey(callId);

            await _redis.HashSetAsync(callKey, new HashEntry("CallState", CallState.Abnormal.ToString()));

            return true;
        }

        public async Task<bool> EndCall(string callId)
        {
            var callKey = CallKey(callId);

            var values = await _redis.HashGetAsync(callKey, new RedisValue[] { "CallerId", "CalleeId" });

            var callerKey = UserKey(long.Parse(values[0]));
            var calleeKey = UserKey(long.Parse(values[1]));

            await _redis.KeyDeleteAsync(callKey);
            await _redis.KeyDeleteAsync(callerKey);
            await _redis.KeyDeleteAsync(calleeKey);

            return true;
        }
    }
}
