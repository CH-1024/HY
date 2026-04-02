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
        readonly ILoginService _loginService;
        readonly IMessageService _messageService;
        readonly IMessageActionService _messageActionService;
        readonly IChatService _chatService;
        readonly IGroupMemberService _groupMemberService;
        readonly IContactService _contactService;


        private record ConnectionKey(long UserId, string HYid, string DevicePlatform);

        // 使用静态字典来保存用户ID和ConnectionId的映射
        //public static readonly ConcurrentDictionary<long, string> _iceMap = new();
        private static readonly ConcurrentDictionary<ConnectionKey, string> _connectionIdMap = new();

        private long _userId => long.TryParse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : throw new Exception("UserId not found in claims");
        private string _hyid => Context.User?.FindFirst("HYid")?.Value ?? throw new Exception("HYid not found in claims");
        private string _deviceId => Context.User?.FindFirst("DeviceId")?.Value ?? throw new Exception("DeviceId not found in claims");
        private string _devicePlatform => Context.User?.FindFirst("DevicePlatform")?.Value ?? throw new Exception("DevicePlatform not found in claims");

        private ConnectionKey? GetConnectionIdMapKey(long userId, string devicePlatform) => _connectionIdMap.Keys.FirstOrDefault(k => k.UserId == userId && k.DevicePlatform == devicePlatform);
        private ConnectionKey? GetConnectionIdMapKey(string hyid, string devicePlatform) => _connectionIdMap.Keys.FirstOrDefault(k => k.HYid == hyid && k.DevicePlatform == devicePlatform);

        private List<ConnectionKey> GetConnectionIdMapKeysByUserId(long userId) => _connectionIdMap.Keys.Where(k => k.UserId == userId).ToList();
        private List<ConnectionKey> GetConnectionIdMapKeysByHyid(string hyid) => _connectionIdMap.Keys.Where(k => k.HYid == hyid).ToList();

        private List<string> GetConnectionIdsByUserId(long userId)
        {
            var keys = GetConnectionIdMapKeysByUserId(userId);
            return keys.Select(k => _connectionIdMap[k]).ToList();
        }

        private List<string> GetConnectionIdsByHYid(string hyid)
        {
            var keys = GetConnectionIdMapKeysByHyid(hyid);
            return keys.Select(k => _connectionIdMap[k]).ToList();
        }



        public ChatHub(ILoginService loginService, IMessageService messageService, IMessageActionService messageActionService, IChatService chatService, IGroupMemberService groupMemberService, IContactService contactService)
        {
            _loginService = loginService;
            _messageService = messageService;
            _messageActionService = messageActionService;
            _chatService = chatService;
            _groupMemberService = groupMemberService;
            _contactService = contactService;
        }


        [Authorize]
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();

            var key = GetConnectionIdMapKey(_userId, _devicePlatform);

            if (key != null && _connectionIdMap.ContainsKey(key))
            {
                var oldConnectionId = _connectionIdMap[key];
                await Clients.Client(oldConnectionId).SendAsync("ForceLogout", "您的账号在其他设备登录了");

                // 如果已经存在，则更新ConnectionId
                _connectionIdMap[key] = Context.ConnectionId;
            }
            else
            {
                key = new ConnectionKey(_userId, _hyid, _devicePlatform);

                // 如果不存在，则添加新的映射
                _connectionIdMap.TryAdd(key, Context.ConnectionId);
                await _loginService.UpdateLoginDeviceOnline(_userId, _deviceId, true);
            }
        }

        [Authorize]
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);

            var key = GetConnectionIdMapKey(_userId, _devicePlatform);

            // 只有当当前断开的连接就是 map 中的连接时才删除
            if (_connectionIdMap.TryGetValue(key, out var storedConnectionId) && storedConnectionId == Context.ConnectionId)
            {
                _connectionIdMap.TryRemove(key, out _);
                await _loginService.UpdateLoginDeviceOnline(_userId, _deviceId, false);
            }
        }


        // InvokeAsync  等待客户端响应  有返回值  同步模式
        // SendAsync    不等待响应      无返回值  异步模式

        [Authorize]
        [HubMethodName("SendMessage")]
        public async Task<long> OnReceiveMessage(MessageDto messageDto)
        {
            // Todo: 联系人验证、黑名单验证、Group群发等

            // 设置消息状态和创建时间
            messageDto.Message_Status = MessageStatus.Sented;
            //messageDto.Created_At = DateTime.UtcNow;

            // 保存消息
            var messageId = await _messageService.InsertMessage(messageDto);
            if (messageId == 0) return 0;

            // 更新会话的最后一条消息
            await _chatService.UpdateChatLastMessage(messageDto);

            if (messageDto.Chat_Type == ChatType.Private)
            {
                // 单人

                // 通知对方所有在线设备
                var receiverConnectionIds = GetConnectionIdsByUserId(messageDto.Target_Id);
                foreach (var receiver in receiverConnectionIds)
                {
                    _ = Clients.Client(receiver).InvokeAsync<bool>("ReceiveMessage", messageDto, CancellationToken.None).ContinueWith(async task =>
                    {
                        if (task.IsCompletedSuccessfully && task.Result)
                        {
                            // 发送成功
                            await _chatService.UpdateChatUnread(messageDto.Target_Id, messageDto.Sender_Id, ChatType.Private);
                        }
                    });
                }
            }
            else if (messageDto.Chat_Type == ChatType.Group)
            {
                // 群聊

                var groupMembers = await _groupMemberService.GetGroupMembersByGroupId(messageDto.Target_Id);

                groupMembers.AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount) // 设置最大并行数
                .ForAll(member =>
                {
                    if (member.User_Id == messageDto.Sender_Id) return; // 排除发送者 Todo: 发送给发送者的其他在线设备

                    var receiverConnectionIds = GetConnectionIdsByUserId(member.User_Id);
                    foreach (var receiver in receiverConnectionIds)
                    {
                        _ = Clients.Client(receiver).InvokeAsync<bool>("ReceiveMessage", messageDto, CancellationToken.None).ContinueWith(async task =>
                        {
                            if (task.IsCompletedSuccessfully && task.Result)
                            {
                                // 发送成功
                                await _chatService.UpdateChatUnread(member.User_Id, messageDto.Target_Id, ChatType.Group);
                            }
                        });
                    }
                });
            }

            return messageId;
        }

        [Authorize]
        [HubMethodName("RecallMessage")]
        public async Task<bool> OnRecallMessage(long messageId)
        {
            var messageDto = await _messageService.GetMessageById(_userId, messageId);
            if (messageDto == null) return false;

            // 只有发送者才能撤回消息
            if (messageDto.Sender_Id != _userId) return false;

            // 撤回消息
            var result = await _messageService.RecallMessage(messageId);
            if (!result) return false;

            // 通知接收者撤回消息
            if (messageDto.Chat_Type == ChatType.Private)
            {
                var receiverConnectionIds = GetConnectionIdsByUserId(messageDto.Target_Id);
                foreach (var receiver in receiverConnectionIds)
                {
                    _ = Clients.Client(receiver).SendAsync("RecallMessage", messageDto);
                }
            }
            else if (messageDto.Chat_Type == ChatType.Group)
            {
                var groupMembers = await _groupMemberService.GetGroupMembersByGroupId(messageDto.Target_Id);

                groupMembers.AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount) // 设置最大并行数
                .ForAll(member =>
                {
                    if (member.User_Id == messageDto.Sender_Id) return; // 排除发送者 Todo: 发送给发送者的其他在线设备

                    var receiverConnectionIds = GetConnectionIdsByUserId(member.User_Id);
                    foreach (var receiver in receiverConnectionIds)
                    {
                        _ = Clients.Client(receiver).SendAsync("RecallMessage", messageDto);
                    }
                });
            }

            return true;
        }

        [Authorize]
        [HubMethodName("DeleteMessage")]
        public async Task<bool> OnDeleteMessage(long messageId)
        {
            var messageDto = await _messageService.GetMessageById(_userId, messageId);
            if (messageDto == null) return false;

            return await _messageActionService.InsertMessageAction(_userId, messageId, MessageActionType.Delete);
        }

        [Authorize]
        [HubMethodName("RequestContact")]
        public async Task<ContactRequestDto?> OnRequestContact(long contactId, string source, string message)
        {
            var contactRequestDto = await _contactService.RequestContact(_userId, contactId, source, message);
            if (contactRequestDto != null)
            {
                // 通知对方所有在线设备
                var receiverConnectionIds = GetConnectionIdsByUserId(contactId);
                foreach (var receiver in receiverConnectionIds)
                {
                    _ = Clients.Client(receiver).SendAsync("RequestContact", contactRequestDto);
                }
            }
            return contactRequestDto;
        }

        [Authorize]
        [HubMethodName("RespondContact")]
        public async Task OnRespondContact(string hyid, bool isAccept, string message)
        {
            var userIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdStr, out var userId)) return;

            //var result = await _contactService.RespondContact(userId, hyid, isAccept, message);
        }

        [Authorize]
        [HubMethodName("ReceiveStream")]
        public async Task<string> ReceiveStream(IAsyncEnumerable<string> stream)
        {
            var result = new StringBuilder();

            await foreach (var item in stream)
            {
                Console.WriteLine($"收到流数据: {item}");
                result.AppendLine(item);
                // 处理流数据...
            }

            return result.ToString();
        }

    }
}
