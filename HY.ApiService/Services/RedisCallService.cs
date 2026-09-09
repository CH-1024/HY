using HY.ApiService.Enums;
using HY.ApiService.Models;
using StackExchange.Redis;
using System.Configuration;
using System.Globalization;
using System.Runtime.InteropServices;

namespace HY.ApiService.Services
{
    public interface IRedisCallService
    {
        Task<bool> IsUserCalling(long userId);
        Task<bool> IsUserPlatformCalling(long userId, int platform);

        Task<CallInfo?> CreateCall(CallType callType, ChatType chatType, long callerId, int callerPlatform, long calleeId);
        Task<CallInfo?> AcceptCall(string callId, long calleeId, int calleePlatform);
        Task<CallInfo?> CancelCall(string callId, long callerId);
        Task<CallInfo?> RejectCall(string callId, long calleeId, int calleePlatform);
        Task<CallInfo?> HangUpCall(string callId, long userId);
        Task<CallInfo?> AbnormalCall(long userId);
        Task<CallInfo?> ExpiryCall(string callId);
    }


    public class RedisCallService : IRedisCallService
    {
        private readonly IConfiguration _configuration;
        private readonly IRedisBaseService _redis;


        public RedisCallService(IConfiguration configuration, IRedisBaseService redis)
        {
            _configuration = configuration;
            _redis = redis;
        }


        private static string CallKey(string callId)
        {
            return $"Call:{callId}";
        }

        private static string UserKey(long userId)
        {
            return $"Call:User:{userId}";
        }

        private string ExpireKey(string callId)
        {
            return $"Call:Expire:{callId}";
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




        public async Task<CallInfo?> CreateCall(CallType callType, ChatType chatType, long callerId, int callerPlatform, long calleeId)
        {
            var sec = _configuration.GetSection("Call:Expire").Value ?? throw new Exception("Call:Expire is not configured");

            var callDto = new CallInfo
            {
                CallId = Guid.NewGuid().ToString("N"),

                CallType = callType,
                ChatType = chatType,

                CallerId = callerId,
                CallerPlatform = callerPlatform,

                CalleeId = calleeId,
                CalleePlatform = -1,        // -1: 未知

                CallState = CallStatus.Calling,

                CreateAt = DateTime.UtcNow,
                ExpiryAt = DateTime.UtcNow.AddSeconds(double.Parse(sec)),
                //StartAt = ,
            };

            var callKey = CallKey(callDto.CallId);
            var callerKey = UserKey(callDto.CallerId);
            var calleeKey = UserKey(callDto.CalleeId);
            var expireKey = ExpireKey(callDto.CallId);

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
                new("ExpiresAt", callDto.ExpiryAt.ToString("O")),
                //new("StartAt", ),
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
            // 建立过期索引
            transaction.StringSetAsync(expireKey, callDto.CallId);

            // 设置过期时间
            //transaction.KeyExpireAsync(callKey, callDto.ExpiryAt);    // 在 ExpireKey 过期时手动清理
            //transaction.KeyExpireAsync(callerKey, callDto.ExpiryAt);  // 在 ExpireKey 过期时手动清理
            //transaction.KeyExpireAsync(calleeKey, callDto.ExpiryAt);  // 在 ExpireKey 过期时手动清理
            transaction.KeyExpireAsync(expireKey, callDto.ExpiryAt);    // ExpireKey 负责触发 expired

            if (!await transaction.ExecuteAsync())
                return null;

            return callDto;
        }

        public async Task<CallInfo?> AcceptCall(string callId, long calleeId, int calleePlatform)
        {
            // callId 必须匹配
            var _callId = await GetCallId(calleeId);
            if (string.IsNullOrEmpty(_callId) || _callId != callId)
                return null;

            // calleeId 必须匹配
            var callDto = await GetCallInfo(callId);
            if (callDto == null || callDto.CalleeId != calleeId)
                return null;

            var callKey = CallKey(callId);
            var utc = DateTime.UtcNow;

            var entries = new HashEntry[]
            {
                new("CalleePlatform", calleePlatform),
                new("CallState", CallStatus.Accepted.ToString()),
                new("StartAt", utc.ToString("O")),
            };

            if (!await _redis.HashSetAsync(callKey, entries))
                return null;

            #region 暂时
            var transaction = _redis.CreateTransaction();
            // 删除通话记录和索引
            transaction.KeyPersistAsync(CallKey(callDto.CallId));
            transaction.KeyPersistAsync(UserKey(callDto.CallerId));
            transaction.KeyPersistAsync(UserKey(callDto.CalleeId));
            transaction.KeyPersistAsync(ExpireKey(callDto.CallId));
            if (!await transaction.ExecuteAsync()) return null;
            #endregion

            callDto.CalleePlatform = calleePlatform;
            callDto.CallState = CallStatus.Accepted;
            callDto.StartAt = utc;

            return callDto;
        }

        public async Task<CallInfo?> CancelCall(string callId, long callerId)
        {
            // callId 必须匹配
            var _callId = await GetCallId(callerId);
            if (string.IsNullOrEmpty(_callId) || _callId != callId)
                return null;

            // callerId 必须匹配
            var callDto = await GetCallInfo(callId);
            if (callDto == null || callDto.CallerId != callerId)
                return null;

            var callKey = CallKey(callId);

            if (!await _redis.HashSetAsync(callKey, new HashEntry("CallState", CallStatus.Cancelled.ToString())))
                return null;

            callDto.CallState = CallStatus.Cancelled;

            if (!await EndCall(callId))
                return null;

            return callDto;
        }

        public async Task<CallInfo?> RejectCall(string callId, long calleeId, int calleePlatform)
        {
            // callId 必须匹配
            var _callId = await GetCallId(calleeId);
            if (string.IsNullOrEmpty(_callId) || _callId != callId)
                return null;

            // calleeId 必须匹配
            var callDto = await GetCallInfo(callId);
            if (callDto == null || callDto.CalleeId != calleeId)
                return null;

            var callKey = CallKey(callId);

            var entries = new HashEntry[]
            {
                new("CalleePlatform", calleePlatform),
                new("CallState", CallStatus.Rejected.ToString())
            };

            if (!await _redis.HashSetAsync(callKey, entries))
                return null;

            callDto.CalleePlatform = calleePlatform;
            callDto.CallState = CallStatus.Rejected;

            if (!await EndCall(callId))
                return null;

            return callDto;
        }

        public async Task<CallInfo?> HangUpCall(string callId, long userId)
        {
            // callId 必须匹配
            var _callId = await GetCallId(userId);
            if (string.IsNullOrEmpty(_callId) || _callId != callId)
                return null;

            // callerId/calleeId 必须匹配
            var callDto = await GetCallInfo(callId);
            if (callDto == null || (callDto.CallerId != userId && callDto.CalleeId != userId))
                return null;

            var callKey = CallKey(callId);

            if(!await _redis.HashSetAsync(callKey, new HashEntry("CallState", CallStatus.Ended.ToString())))
                return null;

            callDto.CallState = CallStatus.Ended;

            if (!await EndCall(callId))
                return null;

            return callDto;
        }

        // 不对外公开，仅 OnDisconnectedAsync 调用
        public async Task<CallInfo?> AbnormalCall(long userId)
        {
            var callId = await GetCallId(userId);

            var callKey = CallKey(callId!);

            if (!await _redis.HashSetAsync(callKey, new HashEntry("CallState", CallStatus.Abnormal.ToString())))
                return null;

            var callDto = await GetCallInfo(callId!);

            if (!await EndCall(callId!))
                return null;

            return callDto;
        }

        // 不对外公开，仅 RedisTimeoutService 调用
        public async Task<CallInfo?> ExpiryCall(string callId)
        {
            var callKey = CallKey(callId);

            if (!await _redis.HashSetAsync(callKey, new HashEntry("CallState", CallStatus.Expired.ToString())))
                return null;

            var callDto = await GetCallInfo(callId);

            if (!await EndCall(callId))
                return null;

            return callDto;
        }

        


        private async Task<bool> EndCall(string callId)
        {
            var callKey = CallKey(callId);
            var expireKey = ExpireKey(callId);

            var values = await _redis.HashGetAsync(callKey, new RedisValue[] { "CallerId", "CalleeId" });

            var callerKey = UserKey(long.Parse(values[0]));
            var calleeKey = UserKey(long.Parse(values[1]));

            var transaction = _redis.CreateTransaction();

            // 删除通话记录和索引
            transaction.KeyDeleteAsync(callKey);
            transaction.KeyDeleteAsync(callerKey);
            transaction.KeyDeleteAsync(calleeKey);
            transaction.KeyDeleteAsync(expireKey);

            return await transaction.ExecuteAsync();
        }

        private async Task<CallInfo?> GetCallInfo(string callId)
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
                "ExpiresAt",
                "StartAt"
            };
            var values = await _redis.HashGetAsync(callKey, redisValues);
            if (values.All(v => string.IsNullOrEmpty(v)))
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
                CreateAt = DateTimeOffset.Parse(values[7]).UtcDateTime,
                ExpiryAt = DateTimeOffset.Parse(values[8]).UtcDateTime,
                StartAt = string.IsNullOrEmpty(values[9]) ? null : DateTimeOffset.Parse(values[9]).UtcDateTime
            };
        }

        private async Task<string?> GetCallId(long userId)
        {
            var userKey = UserKey(userId);

            return await _redis.StringGetAsync(userKey);
        }

    }
}
