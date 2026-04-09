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
    public record ConnectionKey(long UserId, int DevicePlatform);

    public class ChatHub : Hub
    {
        readonly ISqlSugarClient _db;

        readonly ILoginService _loginService;
        readonly IMessageService _messageService;
        readonly IMessageActionService _messageActionService;
        readonly IChatService _chatService;
        readonly IGroupMemberService _groupMemberService;
        readonly IContactService _contactService;



        // 使用静态字典来保存用户ID和ConnectionId的映射
        //public static readonly ConcurrentDictionary<long, string> _iceMap = new();
        private static readonly ConcurrentDictionary<ConnectionKey, string> _connectionIdMap = new();

        private long _userId => long.TryParse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : throw new Exception("UserId not found in claims");
        private string _deviceId => Context.User?.FindFirst("DeviceId")?.Value ?? throw new Exception("DeviceId not found in claims");
        private int _devicePlatform => int.TryParse(Context.User?.FindFirst("DevicePlatform")?.Value, out var platform) ? platform : throw new Exception("DevicePlatform not found in claims");

        private ConnectionKey? GetConnectionIdMapKey(long userId, int devicePlatform) => _connectionIdMap.Keys.FirstOrDefault(k => k.UserId == userId && k.DevicePlatform == devicePlatform);
        private List<string> GetConnectionIdsByUserId(long userId) => _connectionIdMap.Keys.Where(k => k.UserId == userId).Select(k => _connectionIdMap[k]).ToList();


        public ChatHub(ISqlSugarClient db, ILoginService loginService, IMessageService messageService, IMessageActionService messageActionService, IChatService chatService, IGroupMemberService groupMemberService, IContactService contactService)
        {
            _db = db;

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
                key = new ConnectionKey(_userId, _devicePlatform);

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
        public async Task<Response> OnReceiveMessage(MessageDto messageDto)
        {
            // Todo: 联系人验证、黑名单验证、Group群发等

            long? messageId = null;

            // 朋友关系验证
            var contactResult = await _contactService.GetContactByUserId(messageDto.Target_Id, messageDto.Sender_Id);
            if (contactResult.IsSucc && contactResult.Contact.Relation_Status != RelationStatus.Friend)
            {
                return new Response(false, "不是好友关系");
            }

            // 设置消息状态和创建时间
            messageDto.Message_Status = MessageStatus.Sented;
            messageDto.Created_At = DateTime.UtcNow;

            // 开启事务
            var result = await _db.Ado.UseTranAsync(async () =>
            {
                // 保存消息
                messageId = await _messageService.InsertMessage(messageDto);
                if (messageId == 0) throw new Exception("保存消息失败");

                // 更新会话的最后一条消息
                var bol = await _chatService.UpdateChatLastMessage(messageDto);
                if (!bol) throw new Exception("更新会话最后一条消息失败");
            });

            // ---------- 事务结束 ----------
            if (!result.IsSuccess) return new Response(false, "事务处理失败");

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

            return new Response(true)
            {
                Data = new Dictionary<string, object?>
                {
                    { "MessageId", messageId },
                    { "CreatedAt", messageDto.Created_At },
                }
            };
        }

        [Authorize]
        [HubMethodName("RecallMessage")]
        public async Task<Response> OnRecallMessage(long messageId)
        {
            var messageDto = await _messageService.GetMessageById(_userId, messageId);
            if (messageDto == null) return new Response(false, "消息不存在");

            // 只有发送者才能撤回消息
            if (messageDto.Sender_Id != _userId) return new Response(false, "只有发送者才能撤回消息");

            // 撤回消息
            var result = await _messageService.RecallMessage(messageId);
            if (!result) return new Response(false, "撤回消息失败");

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

            return new Response(true);
        }

        [Authorize]
        [HubMethodName("DeleteMessage")]
        public async Task<Response> OnDeleteMessage(long messageId)
        {
            var messageDto = await _messageService.GetMessageById(_userId, messageId);
            if (messageDto == null) return new Response(false, "消息不存在");

            var result = await _messageActionService.InsertMessageAction(_userId, messageId, MessageActionType.Delete);
            if (!result) return new Response(false, "删除消息失败");

            return new Response(true);
        }

        [Authorize]
        [HubMethodName("RequestContact")]
        public async Task<Response> OnRequestContact(long contactId, int source, string message)
        {
            var result = await _contactService.RequestContact(_userId, contactId, source, message);
            if (result == null) return new Response(false, "请求联系人失败");

            // 通知接收方所有在线设备
            var receiverConnectionIds = GetConnectionIdsByUserId(contactId);
            foreach (var receiver in receiverConnectionIds)
            {
                // 一点细节
                _ = Clients.Client(receiver).InvokeAsync<bool>("RequestContact", result.contactRequest, result.receiverContact, result.receiverChat, result.receiverMessage, CancellationToken.None).ContinueWith(async task =>
                {
                    if (task.IsCompletedSuccessfully && task.Result)
                    {
                        await _chatService.UpdateChatUnread(result.receiverMessage.Sender_Id, result.receiverMessage.Target_Id, ChatType.Private);
                    }
                });
            }

            return new Response(true)
            {
                Data = new Dictionary<string, object?>
                {
                    { "ContactRequest", result.contactRequest },
                    { "Contact", result.senderContact },
                    { "Chat", result.senderChat },
                    { "Message", result.senderMessage },
                }
            };
        }

        [Authorize]
        [HubMethodName("RespondContact")]
        public async Task<Response> OnRespondContact(long contactRequestId, RespondContactHandle handle, string message)
        {
            var result = await _contactService.RespondContact(_userId, contactRequestId, handle, message);
            if (result == null) return new Response(false, "处理联系人请求失败");

            if (handle == RespondContactHandle.Revoked)
            {
                // 通知接收方所有在线设备
                var receiverConnectionIds = GetConnectionIdsByUserId(result.contactRequest.Receiver_Id);
                foreach (var receiver in receiverConnectionIds)
                {
                    _ = Clients.Client(receiver).SendAsync("RespondContact", result.contactRequest, null, null, null);
                }

                return new Response(true)
                {
                    Data = new Dictionary<string, object?>
                    {
                        { "ContactRequest", result.contactRequest },
                    }
                };
            }
            else if (handle == RespondContactHandle.Declined)
            {
                // 通知发送方所有在线设备
                var receiverConnectionIds = GetConnectionIdsByUserId(result.contactRequest.Sender_Id);
                foreach (var receiver in receiverConnectionIds)
                {
                    _ = Clients.Client(receiver).SendAsync("RespondContact", result.contactRequest, null, null, null);
                }

                return new Response(true)
                {
                    Data = new Dictionary<string, object?>
                    {
                        { "ContactRequest", result.contactRequest },
                    }
                };
            }
            else if (handle == RespondContactHandle.Accepted)
            {
                // 通知发送方所有在线设备
                var receiverConnectionIds = GetConnectionIdsByUserId(result.contactRequest.Sender_Id);
                foreach (var receiver in receiverConnectionIds)
                {
                    _ = Clients.Client(receiver).SendAsync("RespondContact", result.contactRequest, result.senderContact, result.senderChat, result.senderMessage);
                }

                return new Response(true)
                {
                    Data = new Dictionary<string, object?>
                    {
                        { "ContactRequest", result.contactRequest },
                        { "Contact", result.receiverContact },
                        { "Chat", result.receiverChat },
                        { "Message", result.receiverMessage },
                    }
                };
            }
            else
            {
                return new Response(false, "无效的操作类型");
            }
        }


    }
}
