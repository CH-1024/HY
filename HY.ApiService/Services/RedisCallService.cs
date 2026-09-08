using HY.ApiService.Enums;
using HY.ApiService.Models;
using StackExchange.Redis;
using System.Runtime.InteropServices;

namespace HY.ApiService.Services
{
    public interface IRedisCallService
    {
        Task<bool> IsUserCalling(long userId);
        Task<bool> IsUserPlatformCalling(long userId, int platform);
        Task<string?> GetCallId(long userId);
        Task<CallInfo?> GetCallInfo(string callId);

        Task<bool> CreateCall(CallInfo callDto);
        Task<bool> AcceptCall(string callId, long calleeId, int calleePlatform);
        Task<bool> CancelCall(string callId, long callerId);
        Task<bool> RejectCall(string callId, long calleeId, int calleePlatform);
        Task<bool> HangUpCall(string callId, long userId);
        Task<bool> AbnormalCall(string callId);
        Task<bool> ExpiryCall(string callId);
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
            var callId = await GetCallId(userId);
            if (string.IsNullOrEmpty(callId))
            {
                return false;
            }

            var callDto = await GetCallInfo(callId);

            return callDto!.CallState == CallStatus.Calling &&
                   callDto!.CallState == CallStatus.Accepted &&
                   callDto!.CallState == CallStatus.Connected;
        }

        public async Task<bool> IsUserPlatformCalling(long userId, int platform)
        {
            var callId = await GetCallId(userId);
            if (string.IsNullOrEmpty(callId))
            {
                return false;
            }

            var callDto = await GetCallInfo(callId);
            if ((callDto!.CallerId == userId && callDto!.CallerPlatform == platform) || 
                (callDto!.CalleeId == userId && callDto!.CalleePlatform == platform))
            {
                return callDto.CallState == CallStatus.Calling &&
                       callDto.CallState == CallStatus.Accepted &&
                       callDto.CallState == CallStatus.Connected;
            }

            return false;
        }

        public async Task<string?> GetCallId(long userId)
        {
            var userKey = UserKey(userId);

            return await _redis.StringGetAsync(userKey);
        }

        public async Task<CallInfo?> GetCallInfo(string callId)
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

            return new CallInfo
            {
                CallId = callId,
                CallType = Enum.Parse<CallType>(values[0]),
                ChatType = Enum.Parse<ChatType>(values[1]),
                CallerId = long.Parse(values[2]),
                CallerPlatform = int.Parse(values[3]),
                CalleeId = long.Parse(values[4]),
                CalleePlatform = int.Parse(values[5]),
                CallState = Enum.Parse<CallStatus>(values[6]),
                CreateAt = DateTime.Parse(values[7]),
                ExpiryAt = DateTime.Parse(values[8]),
            };
        }


        public async Task<bool> CreateCall(CallInfo callDto)
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

        public async Task<bool> AcceptCall(string callId, long calleeId, int calleePlatform)
        {
            // callId 必须匹配
            var _callId = await GetCallId(calleeId);
            if (string.IsNullOrEmpty(_callId) || _callId != callId)
                return false;

            // calleeId 必须匹配
            var callDto = await GetCallInfo(callId);
            if (callDto == null || callDto.CalleeId != calleeId)
                return false;

            var callKey = CallKey(callId);

            var entries = new HashEntry[]
            {
                new("CalleePlatform", calleePlatform),
                new("CallState", CallStatus.Accepted.ToString())
            };

            await _redis.HashSetAsync(callKey, entries);

            return true;
        }

        public async Task<bool> CancelCall(string callId, long callerId)
        {
            // callId 必须匹配
            var _callId = await GetCallId(callerId);
            if (string.IsNullOrEmpty(_callId) || _callId != callId)
                return false;

            // callerId 必须匹配
            var callDto = await GetCallInfo(callId);
            if (callDto == null || callDto.CallerId != callerId)
                return false;

            var callKey = CallKey(callId);

            await _redis.HashSetAsync(callKey, new HashEntry("CallState", CallStatus.Cancelled.ToString()));

            return true;
        }

        public async Task<bool> RejectCall(string callId, long calleeId, int calleePlatform)
        {
            // callId 必须匹配
            var _callId = await GetCallId(calleeId);
            if (string.IsNullOrEmpty(_callId) || _callId != callId)
                return false;

            // calleeId 必须匹配
            var callDto = await GetCallInfo(callId);
            if (callDto == null || callDto.CalleeId != calleeId)
                return false;

            var callKey = CallKey(callId);

            var entries = new HashEntry[]
            {
                new("CalleePlatform", calleePlatform),
                new("CallState", CallStatus.Rejected.ToString())
            };

            await _redis.HashSetAsync(callKey, entries);

            return true;
        }

        public async Task<bool> HangUpCall(string callId, long userId)
        {
            // callId 必须匹配
            var _callId = await GetCallId(userId);
            if (string.IsNullOrEmpty(_callId) || _callId != callId)
                return false;

            var callKey = CallKey(callId);

            await _redis.HashSetAsync(callKey, new HashEntry("CallState", CallStatus.Ended.ToString()));

            return true;
        }

        // 不对外公开，仅 OnDisconnectedAsync 调用
        public async Task<bool> AbnormalCall(string callId)
        {
            var callKey = CallKey(callId);

            await _redis.HashSetAsync(callKey, new HashEntry("CallState", CallStatus.Abnormal.ToString()));

            return true;
        }

        // 不对外公开，仅 RedisTimeoutService 调用
        public async Task<bool> ExpiryCall(string callId)
        {
            var callKey = CallKey(callId);

            await _redis.HashSetAsync(callKey, new HashEntry("CallState", CallStatus.Expired.ToString()));

            return true;
        }


        private async Task<bool> EndCall(string callId)
        {
            var callKey = CallKey(callId);

            var values = await _redis.HashGetAsync(callKey, new RedisValue[] { "CallerId", "CalleeId" });

            var callerKey = UserKey(long.Parse(values[0]));
            var calleeKey = UserKey(long.Parse(values[1]));

            var transaction = _redis.CreateTransaction();

            // 双方都必须有正在进行的通话
            transaction.AddCondition(Condition.KeyExists(callerKey));
            transaction.AddCondition(Condition.KeyExists(calleeKey));

            // 删除通话记录和索引
            transaction.KeyDeleteAsync(callKey);
            transaction.KeyDeleteAsync(callerKey);
            transaction.KeyDeleteAsync(calleeKey);

            return await transaction.ExecuteAsync();
        }

        //private async Task UpdateCallState(string callId, CallState newState)
        //{
        //    var callKey = CallKey(callId);

        //     await _redis.HashSetAsync(callKey, new HashEntry("CallState", newState.ToString()));
        //}

    }
}
