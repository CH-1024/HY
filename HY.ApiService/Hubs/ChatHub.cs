using Dm.filter;
using HY.ApiService.Dtos;
using HY.ApiService.Entities;
using HY.ApiService.Enums;
using HY.ApiService.Hubs.Requests;
using HY.ApiService.Models;
using HY.ApiService.Repositories;
using HY.ApiService.Services;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.SignalR;
using SqlSugar;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Security.Claims;
using System.Text;

namespace HY.ApiService.Hubs
{
    public class ChatHub : Hub
    {
        readonly IConfiguration _configuration;
        readonly IRedisConnectionService _redisConnectionService;
        readonly IRedisCallService _redisCallService;
        readonly IChatNotificationService _chatNotificationService;
        
        readonly ILoginService _loginService;
        readonly IContactService _contactService;
        readonly IGroupMemberService _groupMemberService;


        private long _userId => long.TryParse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : throw new Exception("UserId not found in claims");
        private string _deviceId => Context.User?.FindFirst("DeviceId")?.Value ?? throw new Exception("DeviceId not found in claims");
        private int _devicePlatform => int.TryParse(Context.User?.FindFirst("DevicePlatform")?.Value, out var platform) ? platform : throw new Exception("DevicePlatform not found in claims");


        public ChatHub(IConfiguration configuration, IRedisConnectionService redisConnectionService, IRedisCallService redisCallService, IChatNotificationService chatNotificationService, ILoginService loginService, IContactService contactService, IGroupMemberService groupMemberService)
        {
            _configuration = configuration;
            _redisConnectionService = redisConnectionService;
            _redisCallService = redisCallService;
            _chatNotificationService = chatNotificationService;

            _loginService = loginService;
            _contactService = contactService;
            _groupMemberService = groupMemberService;
        }


        [Authorize]
        public override async Task OnConnectedAsync()
        {
            var userId = _userId;
            var deviceId = _deviceId;
            var platform = _devicePlatform;

            var isOnline = await _redisConnectionService.IsOnlineAsync(userId);
            if (!isOnline)
            {
                await _loginService.UpdateLoginDeviceOnline(userId, deviceId, true);
            }

            var oldConnectionId = await _redisConnectionService.GetConnectionIdAsync(userId, platform);
            if (!string.IsNullOrEmpty(oldConnectionId) && oldConnectionId != Context.ConnectionId)
            {
                await Clients.Client(oldConnectionId).SendAsync("ForceLogout", "您的账号在其他设备登录了", CancellationToken.None);
            }

            await _redisConnectionService.SetConnectionAsync(userId, platform, Context.ConnectionId);

            await base.OnConnectedAsync();
        }

        [Authorize]
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = _userId;
            var deviceId = _deviceId;
            var platform = _devicePlatform;

            await _redisConnectionService.RemoveConnectionAsync(userId, platform, Context.ConnectionId);

            var isOnline = await _redisConnectionService.IsOnlineAsync(userId);
            if (!isOnline)
            {
                await _loginService.UpdateLoginDeviceOnline(userId, deviceId, false);
            }

            var isCalling = await _redisCallService.IsUserPlatformCalling(userId, platform);
            if (isCalling)
            {
                var callId = await _redisCallService.GetCallId(userId);

                await _redisCallService.AbnormalCall(callId!);

                var callDto = await _redisCallService.GetCall(callId!);

                var chatType = callDto!.ChatType;
                var anotherUserId = userId == callDto.CallerId ? callDto.CalleeId : callDto.CallerId;
                var anotherUserPlatform = userId == callDto.CallerId ? callDto.CalleePlatform : callDto.CallerPlatform;
                // 通知接收方
                await _chatNotificationService.AbnormalCallNotify(chatType, callId!, anotherUserId, anotherUserPlatform);
            }

            await base.OnDisconnectedAsync(exception);
        }


        // InvokeAsync  等待客户端响应  有返回值  同步模式
        // SendAsync    不等待响应      无返回值  异步模式

        public async Task<Response> CreateCall(CreateCallRequest request)
        {
            var callerId = _userId;
            var callerPlatform = _devicePlatform;

            var callType = request.CallType;
            var chatType = request.ChatType;
            var calleeId = request.CalleeId;

            if (callerId == calleeId)
            {
                return new Response(false, "不能呼叫自己");
            }
            if (chatType == ChatType.Private)
            {
                // 私聊
                // 联系人验证
                var contactResult = await _contactService.GetContactByUserId(calleeId, callerId);
                if (contactResult.IsSucc && contactResult.Contact!.Relation_Status != RelationStatus.Friend)
                {
                    return new Response(false, "不是好友关系");
                }
            }
            else if (chatType == ChatType.Group)
            {
                // 群聊
                // 群成员验证
                var groupMemberResult = await _groupMemberService.GetGroupMember(calleeId, callerId);
                if (groupMemberResult == null)
                {
                    return new Response(false, "不是群成员");
                }
            }
            else
            {
                return new Response(false, "类型异常");
            }

            var sec = _configuration.GetSection("Call:Expire").Value ?? throw new Exception("Call:Expire is not configured");

            var callDto = new CallDto
            {
                CallId = Guid.NewGuid().ToString("N"),

                CallType = callType,
                ChatType = chatType,

                CallerId = callerId,
                CallerPlatform = callerPlatform,

                CalleeId = calleeId,
                CalleePlatform = -1,        // -1: 未知

                CallState = CallState.Calling,

                CreateAt = DateTime.UtcNow,
                ExpiryAt = DateTime.UtcNow.AddSeconds(double.Parse(sec)),
            };
            var bol = await _redisCallService.CreateCall(callDto);
            if (!bol)
            {
                return new Response(false, "创建通话失败");
            }

            // 通知接收方
            await _chatNotificationService.CreateCallNotify(callDto);

            return new Response(true)
            {
                Data = new Dictionary<string, object?>
                {
                    { "CallId",  callDto.CallId},
                    { "ExpiryAt",  callDto.ExpiryAt},
                }
            };
        }

        public async Task<Response> CancelCall(string callId)
        {
            var callerId = _userId;
            var callerPlatform = _devicePlatform;

            var bol = await _redisCallService.CancelCall(callId, callerId, callerPlatform);
            if (!bol)
            {
                return new Response(false, "取消通话失败");
            }

            var callDto = await _redisCallService.GetCall(callId);

            // 通知接收方
            await _chatNotificationService.CancelCallNotify(callDto!);

            return new Response(true);
        }

        public async Task<Response> AcceptCall(string callId)
        {
            var calleeId = _userId;
            var calleePlatform = _devicePlatform;

            var bol = await _redisCallService.AcceptCall(callId, calleeId, calleePlatform);
            if (!bol)
            {
                return new Response(false, "接听通话失败");
            }

            var callDto = await _redisCallService.GetCall(callId);

            // 通知接收方
            var acceptResult = await _chatNotificationService.AcceptCallNotify(callDto!);

            return new Response(acceptResult);
        }

        public async Task<Response> RejectCall(string callId)
        {
            var calleeId = _userId;
            var calleePlatform = _devicePlatform;

            var bol = await _redisCallService.RejectCall(callId, calleeId, calleePlatform);
            if (!bol)
            {
                return new Response(false, "拒绝通话失败");
            }

            var callDto = await _redisCallService.GetCall(callId);

            // 通知接收方
            await _chatNotificationService.RejectCallNotify(callDto!);

            return new Response(true);
        }

        public async Task<Response> HangUpCall(string callId)
        {
            var userId = _userId;

            var bol = await _redisCallService.HangUpCall(callId, userId);
            if (!bol)
            {
                return new Response(false, "挂断通话失败");
            }

            var callDto = await _redisCallService.GetCall(callId);

            var chatType = callDto!.ChatType;
            var anotherUserId = userId == callDto.CallerId ? callDto.CalleeId : callDto.CallerId;
            var anotherUserPlatform = userId == callDto.CallerId ? callDto.CalleePlatform : callDto.CallerPlatform;

            // 通知接收方
            await _chatNotificationService.HangUpCallNotify(chatType, callId!, anotherUserId, anotherUserPlatform);

            return new Response(true);
        }

    }
}
