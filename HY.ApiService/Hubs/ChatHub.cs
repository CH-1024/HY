using Dm.filter;
using HY.ApiService.Dtos;
using HY.ApiService.Entities;
using HY.ApiService.Enums;
using HY.ApiService.Repositories;
using HY.ApiService.Services;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
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


        // 使用静态字典来保存用户ID和ConnectionId的映射
        //public static readonly ConcurrentDictionary<long, string> _iceMap = new();
        public static readonly ConcurrentDictionary<(long UserId, string DevicePlatform), string> _connectionIdMap = new();


        public ChatHub(ILoginService loginService, IMessageService messageService, IMessageActionService messageActionService, IChatService chatService, IGroupMemberService groupMemberService)
        {
            _loginService = loginService;
            _messageService = messageService;
            _messageActionService = messageActionService;
            _chatService = chatService;
            _groupMemberService = groupMemberService;
        }


        [Authorize]
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();

            var userIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var deviceId = Context.User?.FindFirst("DeviceId")?.Value!;
            var devicePlatform = Context.User?.FindFirst("DevicePlatform")?.Value!;

            if (long.TryParse(userIdStr, out var userId))
            {
                var key = (userId, devicePlatform);

                if (_connectionIdMap.ContainsKey(key))
                {
                    var oldConnectionId = _connectionIdMap[key];
                    await Clients.Client(oldConnectionId).SendAsync("ForceLogout", "您的账号在其他设备登录了");

                    // 如果已经存在，则更新ConnectionId
                    _connectionIdMap[key] = Context.ConnectionId;
                }
                else
                {
                    // 如果不存在，则添加新的映射
                    _connectionIdMap.TryAdd(key, Context.ConnectionId);
                    await _loginService.UpdateLoginDeviceOnline(userId, deviceId, true);
                }
            }
        }

        [Authorize]
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);

            var userIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var deviceId = Context.User?.FindFirst("DeviceId")?.Value!;
            var devicePlatform = Context.User?.FindFirst("DevicePlatform")?.Value!;

            if (long.TryParse(userIdStr, out var userId))
            {
                var key = (userId, devicePlatform);

                // 只有当当前断开的连接就是 map 中的连接时才删除
                if (_connectionIdMap.TryGetValue(key, out var storedConnectionId) && storedConnectionId == Context.ConnectionId)
                {
                    _connectionIdMap.TryRemove(key, out _);
                    await _loginService.UpdateLoginDeviceOnline(userId, deviceId, false);
                }
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

                var targetKeys = _connectionIdMap.Keys.Where(k => k.UserId == messageDto.Target_Id);

                var receiverConnectionIds = targetKeys.Select(k => _connectionIdMap[k]).ToList();

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
                    if (member.User_Id == messageDto.Sender_Id) return; // 排除发送者 Todo: 可以考虑发送给发送者自己

                    var memberKeys = _connectionIdMap.Keys.Where(k => k.UserId == member.User_Id);

                    var receiverConnectionIds = memberKeys.Select(k => _connectionIdMap[k]).ToList();

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
            var userIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdStr, out var currentUserId)) return false;

            var messageDto = await _messageService.GetMessageById(currentUserId, messageId);
            if (messageDto == null) return false;

            // 只有发送者才能撤回消息
            if (messageDto.Sender_Id != currentUserId) return false;

            // 撤回消息
            var result = await _messageService.RecallMessage(messageId);
            if (!result) return false;

            // 通知接收者撤回消息
            if (messageDto.Chat_Type == ChatType.Private)
            {
                var targetKeys = _connectionIdMap.Keys.Where(k => k.UserId == messageDto.Target_Id);

                var receiverConnectionIds = targetKeys.Select(k => _connectionIdMap[k]).ToList();

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
                    if (member.User_Id == messageDto.Sender_Id) return; // 排除发送者

                    var memberKeys = _connectionIdMap.Keys.Where(k => k.UserId == member.User_Id);

                    var receiverConnectionIds = memberKeys.Select(k => _connectionIdMap[k]).ToList();

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
            var userIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdStr, out var currentUserId)) return false;

            var messageDto = await _messageService.GetMessageById(currentUserId, messageId);
            if (messageDto == null) return false;

            return await _messageActionService.InsertMessageAction(currentUserId, messageId, MessageActionType.Delete);
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
