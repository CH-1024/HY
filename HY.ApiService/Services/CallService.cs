using HY.ApiService.Dtos;
using HY.ApiService.Enums;
using HY.ApiService.Models;

namespace HY.ApiService.Services
{
    public interface ICallService
    {
        Task<Response> CreateCall(long callerId, CallType callType, ChatType targetType, long targetId);
        Task AcceptCall(string v, string callId);
        Task RejectCall(string v, string callId);
        Task HangUpCall(string v, string callId);
        Task CancelCall(string v, string callId);
    }

    public class CallService : ICallService
    {
        readonly IChatNotificationService _chatNotificationService;
        readonly IRedisConnectionService _redisConnectionService;

        readonly IContactService _contactService;
        readonly IGroupMemberService _groupMemberService;

        public CallService(IChatNotificationService chatNotificationService, IRedisConnectionService redisConnectionService, IContactService contactService, IGroupMemberService groupMemberService)
        {
            _chatNotificationService = chatNotificationService;
            _redisConnectionService = redisConnectionService;

            _contactService = contactService;
            _groupMemberService = groupMemberService;
        }



        public async Task<Response> CreateCall(long callerId, CallType callType, ChatType targetType, long targetId)
        {
            if (targetType == ChatType.Private)
            {
                // 私聊
                // 联系人验证
                var contactResult = await _contactService.GetContactByUserId(targetId, callerId);
                if (contactResult.IsSucc && contactResult.Contact!.Relation_Status != RelationStatus.Friend)
                {
                    return new Response(false, "不是好友关系");
                }
            }
            else if (targetType == ChatType.Group)
            {
                // 群聊
                // 群成员验证
                var groupMemberResult = await _groupMemberService.GetGroupMember(targetId, callerId);
                if (groupMemberResult == null)
                {
                    return new Response(false, "不是群成员");
                }
            }
            else
            {
                return new Response(false, "类型异常");
            }

            // 2. 通知接收方
            await _chatNotificationService.CreateCallNotification(callerId, callType, targetType, targetId);

            return new Response(true);
        }

        public Task AcceptCall(string v, string callId)
        {
            throw new NotImplementedException();
        }

        public Task RejectCall(string v, string callId)
        {
            throw new NotImplementedException();
        }

        public Task HangUpCall(string v, string callId)
        {
            throw new NotImplementedException();
        }

        public Task CancelCall(string v, string callId)
        {
            throw new NotImplementedException();
        }

    }
}
