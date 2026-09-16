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
using System.Diagnostics;
using System.Security.Claims;
using System.Text;

namespace HY.ApiService.Hubs
{
    public class ChatHub : Hub
    {
        readonly IRedisConnectionService _redisConnectionService;
        readonly IRedisCallService _redisCallService;
        readonly IChatNotificationService _chatNotificationService;
        
        readonly ILoginService _loginService;
        readonly IContactService _contactService;
        readonly IGroupMemberService _groupMemberService;


        private long _userId => long.TryParse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : throw new Exception("UserId not found in claims");
        private string _deviceId => Context.User?.FindFirst("DeviceId")?.Value ?? throw new Exception("DeviceId not found in claims");
        private int _devicePlatform => int.TryParse(Context.User?.FindFirst("DevicePlatform")?.Value, out var platform) ? platform : throw new Exception("DevicePlatform not found in claims");


        public ChatHub(IRedisConnectionService redisConnectionService, IRedisCallService redisCallService, IChatNotificationService chatNotificationService, ILoginService loginService, IContactService contactService, IGroupMemberService groupMemberService)
        {
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
                var callDto = await _redisCallService.AbnormalCall(userId!);

                // 通知接收方
                await _chatNotificationService.AbnormalCallNotify(callDto!, userId);
            }

            await base.OnDisconnectedAsync(exception);
        }


        // InvokeAsync  等待客户端响应  有返回值  同步模式
        // SendAsync    不等待响应      无返回值  异步模式


        [Authorize]
        public async Task SendIce(string callId, string ice)
        {
            var userId = _userId;

            Console.WriteLine($"SendIce : {userId}");

            var callInfo = await _redisCallService.GetCallInfo(callId);
            if (callInfo == null)
            {
                return;
            }

            var targetId = userId == callInfo.CalleeId ? callInfo.CallerId : callInfo.CalleeId;
            var targetPlatform = userId == callInfo.CalleeId ? callInfo.CallerPlatform : callInfo.CalleePlatform;

            var connectionId = await _redisConnectionService.GetConnectionIdAsync(targetId, targetPlatform);
            if (!string.IsNullOrEmpty(connectionId)) await Clients.Client(connectionId).SendAsync("ReceiveIce", ice);

            return;
        }

        [Authorize]
        public async Task SendOffer(string callId, string offer)
        {
            var userId = _userId;

            Debug.WriteLine($"SendOffer : {userId}");

            var callInfo = await _redisCallService.GetCallInfo(callId);
            if (callInfo == null)
            {
                return;
            }

            var targetId = userId == callInfo.CalleeId ? callInfo.CallerId : callInfo.CalleeId;
            var targetPlatform = userId == callInfo.CalleeId ? callInfo.CallerPlatform : callInfo.CalleePlatform;

            var connectionId = await _redisConnectionService.GetConnectionIdAsync(targetId, targetPlatform);
            if (!string.IsNullOrEmpty(connectionId)) await Clients.Client(connectionId).SendAsync("ReceiveOffer", offer);

            return;
        }

        [Authorize]
        public async Task SendAnswer(string callId, string answer)
        {
            var userId = _userId;

            Debug.WriteLine($"SendAnswer : {userId}");

            var callInfo = await _redisCallService.GetCallInfo(callId);
            if (callInfo == null)
            {
                return;
            }

            var targetId = userId == callInfo.CalleeId ? callInfo.CallerId : callInfo.CalleeId;
            var targetPlatform = userId == callInfo.CalleeId ? callInfo.CallerPlatform : callInfo.CalleePlatform;

            var connectionId = await _redisConnectionService.GetConnectionIdAsync(targetId, targetPlatform);
            if (!string.IsNullOrEmpty(connectionId)) await Clients.Client(connectionId).SendAsync("ReceiveAnswer", answer);

            return;
        }


    }
}
