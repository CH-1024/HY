using Dm.filter;
using HY.ApiService.Dtos;
using HY.ApiService.Entities;
using HY.ApiService.Enums;
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
using System.Security.Claims;
using System.Text;

namespace HY.ApiService.Hubs
{
    public class ChatHub : Hub
    {
        readonly IRedisConnectionService _redisConnectionService;
        readonly IChatNotificationService _chatNotificationService;
        readonly ICallService _callService;
        
        readonly ILoginService _loginService;
        readonly IContactService _contactService;
        readonly IGroupMemberService _groupMemberService;


        private long _userId => long.TryParse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : throw new Exception("UserId not found in claims");
        private string _deviceId => Context.User?.FindFirst("DeviceId")?.Value ?? throw new Exception("DeviceId not found in claims");
        private int _devicePlatform => int.TryParse(Context.User?.FindFirst("DevicePlatform")?.Value, out var platform) ? platform : throw new Exception("DevicePlatform not found in claims");


        public ChatHub(IRedisConnectionService redisConnectionService, IChatNotificationService chatNotificationService, ICallService callService, ILoginService loginService, IContactService contactService, IGroupMemberService groupMemberService)
        {
            _redisConnectionService = redisConnectionService;
            _chatNotificationService = chatNotificationService;
            _callService = callService;

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
                await Clients.Client(oldConnectionId).SendAsync("ForceLogout", "您的账号在其他设备登录了");
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

            await base.OnDisconnectedAsync(exception);
        }


        // InvokeAsync  等待客户端响应  有返回值  同步模式
        // SendAsync    不等待响应      无返回值  异步模式

        public async Task<Response> CreateCall(CallType callType, ChatType chatType, long calleeId)
        {
            var callerId = _userId;

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

            // 2. 通知接收方
            await _chatNotificationService.CreateCallNotification(callerId, callType, chatType, calleeId);

            return new Response(true);
        }

        public async Task<Response> CancelCall(CallType callType, ChatType chatType, long calleeId)
        {
            var callerId = _userId;

            //if (chatType == ChatType.Private)
            //{
            //    // 私聊
            //    // 联系人验证
            //    var contactResult = await _contactService.GetContactByUserId(calleeId, callerId);
            //    if (contactResult.IsSucc && contactResult.Contact!.Relation_Status != RelationStatus.Friend)
            //    {
            //        return new Response(false, "不是好友关系");
            //    }
            //}
            //else if (chatType == ChatType.Group)
            //{
            //    // 群聊
            //    // 群成员验证
            //    var groupMemberResult = await _groupMemberService.GetGroupMember(calleeId, callerId);
            //    if (groupMemberResult == null)
            //    {
            //        return new Response(false, "不是群成员");
            //    }
            //}
            //else
            //{
            //    return new Response(false, "类型异常");
            //}

            // 2. 通知接收方
            await _chatNotificationService.CancelCallNotification(callerId, callType, chatType, calleeId);

            return new Response(true);
        }

        public async Task<Response> AcceptCall(CallType callType, ChatType chatType, long callerId)
        {
            var calleeId = _userId;

            //if (chatType == ChatType.Private)
            //{
            //    // 私聊
            //    // 联系人验证
            //    var contactResult = await _contactService.GetContactByUserId(callerId, calleeId);
            //    if (contactResult.IsSucc && contactResult.Contact!.Relation_Status != RelationStatus.Friend)
            //    {
            //        return new Response(false, "不是好友关系");
            //    }
            //}
            //else if (chatType == ChatType.Group)
            //{
            //    // 群聊
            //    // 群成员验证
            //    var groupMemberResult = await _groupMemberService.GetGroupMember(callerId, calleeId);
            //    if (groupMemberResult == null)
            //    {
            //        return new Response(false, "不是群成员");
            //    }
            //}
            //else
            //{
            //    return new Response(false, "类型异常");
            //}

            // 2. 通知接收方
            await _chatNotificationService.AcceptCallNotification(callerId, callType, chatType, calleeId);

            return new Response(true);
        }

        public async Task<Response> RejectCall(CallType callType, ChatType chatType, long callerId)
        {
            var calleeId = _userId;

            //if (chatType == ChatType.Private)
            //{
            //    // 私聊
            //    // 联系人验证
            //    var contactResult = await _contactService.GetContactByUserId(callerId, calleeId);
            //    if (contactResult.IsSucc && contactResult.Contact!.Relation_Status != RelationStatus.Friend)
            //    {
            //        return new Response(false, "不是好友关系");
            //    }
            //}
            //else if (chatType == ChatType.Group)
            //{
            //    // 群聊
            //    // 群成员验证
            //    var groupMemberResult = await _groupMemberService.GetGroupMember(callerId, calleeId);
            //    if (groupMemberResult == null)
            //    {
            //        return new Response(false, "不是群成员");
            //    }
            //}
            //else
            //{
            //    return new Response(false, "类型异常");
            //}

            // 2. 通知接收方
            await _chatNotificationService.RejectCallNotification(callerId, callType, chatType, calleeId);

            return new Response(true);
        }

        public async Task HangUpCall(string callId)
        {
            await _callService.HangUpCall(Context.UserIdentifier!, callId);
        }

    }
}
