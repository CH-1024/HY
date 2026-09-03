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
        readonly ILoginService _loginService;
        readonly IChatService _chatService;
        readonly IGroupMemberService _groupMemberService;



        // 使用静态字典来保存用户ID和ConnectionId的映射
        //public static readonly ConcurrentDictionary<long, string> _iceMap = new();
        private static readonly ConcurrentDictionary<ConnectionKey, string> _connectionIdMap = new();

        private long _userId => long.TryParse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : throw new Exception("UserId not found in claims");
        private string _deviceId => Context.User?.FindFirst("DeviceId")?.Value ?? throw new Exception("DeviceId not found in claims");
        private int _devicePlatform => int.TryParse(Context.User?.FindFirst("DevicePlatform")?.Value, out var platform) ? platform : throw new Exception("DevicePlatform not found in claims");

        private ConnectionKey? GetConnectionIdMapKey(long userId, int devicePlatform) => _connectionIdMap.Keys.FirstOrDefault(k => k.UserId == userId && k.DevicePlatform == devicePlatform);
        private List<string> GetAllPlatformConnectionIds(long userId) => _connectionIdMap.Keys.Where(k => k.UserId == userId).Select(k => _connectionIdMap[k]).ToList();
        private List<string> GetOtherPlatformConnectionIds(long userId, int devicePlatform) => _connectionIdMap.Keys.Where(k => k.UserId == userId && k.DevicePlatform != devicePlatform).Select(k => _connectionIdMap[k]).ToList();


        public ChatHub(ILoginService loginService, IChatService chatService, IGroupMemberService groupMemberService)
        {
            _loginService = loginService;
            _chatService = chatService;
            _groupMemberService = groupMemberService;
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
        public async Task OnReceiveMessageNotice(MessageDto messageDto)
        {
            if (messageDto == null)
            {
                return;
            }

            if (messageDto.Chat_Type == ChatType.Private)
            {
                // 单人

                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 20,
                    CancellationToken = CancellationToken.None
                };

                var receiverConnectionIds = GetAllPlatformConnectionIds(messageDto.Target_Id);
                var otherPlatformConnectionIds = GetOtherPlatformConnectionIds(messageDto.Sender_Id, _devicePlatform);

                #region 通知对方所有在线设备
                var sendResults = new ConcurrentBag<bool>();

                await Parallel.ForEachAsync(receiverConnectionIds, parallelOptions, async (connectionId, cancellationToken) =>
                {
                    try
                    {
                        var success = await Clients.Client(connectionId).InvokeAsync<bool>("ReceiveMessage", messageDto, cancellationToken);

                        sendResults.Add(success);
                    }
                    catch (Exception ex)
                    {
                        // 记录日志
                        // _logger.LogError(ex, "发送消息失败，ConnectionId: {ConnectionId}", connectionId);

                        sendResults.Add(false);
                    }
                });

                // 至少有一个接收成功，更新未读数
                if (sendResults.Any(x => x))
                {
                    await _chatService.ClearChatUnread(messageDto.Target_Id, messageDto.Sender_Id, ChatType.Private);
                }
                #endregion


                #region 通知自己其他在线设备
                await Parallel.ForEachAsync(otherPlatformConnectionIds, parallelOptions, async (connectionId, cancellationToken) =>
                {
                    try
                    {
                        await Clients.Client(connectionId).InvokeAsync<bool>("ReceiveMessage", messageDto, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        // 记录日志
                        // _logger.LogError(ex, "发送消息失败，ConnectionId: {ConnectionId}", connectionId);
                    }
                });

                // 自己发的消息不需要更新未读数
                #endregion
            }
            else if (messageDto.Chat_Type == ChatType.Group)
            {
                // 群聊

                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 20,
                    CancellationToken = CancellationToken.None
                };

                // 获取群成员列表
                var groupMembers = await _groupMemberService.GetGroupMembersByGroupId(messageDto.Target_Id);

                var receiverConnections = new List<(long UserId, string ConnectionId)>();
                var otherPlatformConnections = new List<(long UserId, string ConnectionId)>();

                foreach (var member in groupMembers)
                {
                    if (member.User_Id == messageDto.Sender_Id)
                    {
                        // 自己其他在线设备
                        var connectionIds = GetOtherPlatformConnectionIds(member.User_Id, _devicePlatform);
                        otherPlatformConnections.AddRange(connectionIds.Select(connId => (UserId: member.User_Id, ConnectionId: connId)));
                    }
                    else
                    {
                        // 成员所有在线设备
                        var connectionIds = GetAllPlatformConnectionIds(member.User_Id);
                        receiverConnections.AddRange(connectionIds.Select(connId => (UserId: member.User_Id, ConnectionId: connId)));
                    }
                }

                #region 通知成员所有在线设备
                foreach (var group in receiverConnections.GroupBy(r => r.UserId))
                {
                    var sendResults = new ConcurrentBag<bool>();

                    await Parallel.ForEachAsync(group.Select(r => r.ConnectionId), parallelOptions, async (connectionId, cancellationToken) =>
                    {
                        try
                        {
                            var success = await Clients.Client(connectionId).InvokeAsync<bool>("ReceiveMessage", messageDto, cancellationToken);

                            sendResults.Add(success);
                        }
                        catch (Exception ex)
                        {
                            // 记录日志
                            // _logger.LogError(ex, "发送消息失败，ConnectionId: {ConnectionId}", connectionId);

                            sendResults.Add(false);
                        }
                    });

                    // 至少有一个接收成功，更新未读数
                    if (sendResults.Any(x => x))
                    {
                        await _chatService.ClearChatUnread(group.Key, messageDto.Target_Id, ChatType.Group);
                    }
                }
                #endregion


                #region 通知自己其他在线设备
                await Parallel.ForEachAsync(otherPlatformConnections.Select(r => r.ConnectionId), parallelOptions, async (connectionId, cancellationToken) =>
                {
                    try
                    {
                        await Clients.Client(connectionId).InvokeAsync<bool>("ReceiveMessage", messageDto, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        // 记录日志
                        // _logger.LogError(ex, "发送消息失败，ConnectionId: {ConnectionId}", connectionId);
                    }
                });

                // 自己发的消息不需要更新未读数
                #endregion
            }
        }

        [Authorize]
        [HubMethodName("RecallMessage")]
        public async Task OnRecallMessageNotice(MessageDto messageDto)
        {
            if (messageDto == null)
            {
                return;
            }

            // 通知接收者撤回消息
            if (messageDto.Chat_Type == ChatType.Private)
            {
                // 单人

                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 20,
                    CancellationToken = CancellationToken.None
                };

                var receiverConnectionIds = GetAllPlatformConnectionIds(messageDto.Target_Id);
                var otherPlatformConnectionIds = GetOtherPlatformConnectionIds(messageDto.Sender_Id, _devicePlatform);

                #region 通知对方所有在线设备
                await Parallel.ForEachAsync(receiverConnectionIds, parallelOptions, async (connectionId, cancellationToken) =>
                {
                    try
                    {
                        await Clients.Client(connectionId).SendAsync("RecallMessage", messageDto);
                    }
                    catch (Exception ex)
                    {
                        // 记录日志
                        // _logger.LogError(ex, "撤回消息失败，ConnectionId: {ConnectionId}", connectionId);
                    }
                });
                #endregion


                #region 通知自己其他在线设备
                await Parallel.ForEachAsync(otherPlatformConnectionIds, parallelOptions, async (connectionId, cancellationToken) =>
                {
                    try
                    {
                        await Clients.Client(connectionId).SendAsync("RecallMessage", messageDto);
                    }
                    catch (Exception ex)
                    {
                        // 记录日志
                        // _logger.LogError(ex, "撤回消息失败，ConnectionId: {ConnectionId}", connectionId);
                    }
                });
                #endregion
            }
            else if (messageDto.Chat_Type == ChatType.Group)
            {
                // 群聊
                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 20,
                    CancellationToken = CancellationToken.None
                };

                // 获取群成员列表
                var groupMembers = await _groupMemberService.GetGroupMembersByGroupId(messageDto.Target_Id);

                var receiverConnections = new List<(long UserId, string ConnectionId)>();
                var otherPlatformConnections = new List<(long UserId, string ConnectionId)>();

                foreach (var member in groupMembers)
                {
                    if (member.User_Id == messageDto.Sender_Id)
                    {
                        // 自己其他在线设备
                        var connectionIds = GetOtherPlatformConnectionIds(member.User_Id, _devicePlatform);
                        otherPlatformConnections.AddRange(connectionIds.Select(connId => (UserId: member.User_Id, ConnectionId: connId)));
                    }
                    else
                    {
                        // 成员所有在线设备
                        var connectionIds = GetAllPlatformConnectionIds(member.User_Id);
                        receiverConnections.AddRange(connectionIds.Select(connId => (UserId: member.User_Id, ConnectionId: connId)));
                    }
                }

                #region 通知成员所有在线设备
                foreach (var group in receiverConnections.GroupBy(r => r.UserId))
                {
                    await Parallel.ForEachAsync(group.Select(r => r.ConnectionId), parallelOptions, async (connectionId, cancellationToken) =>
                    {
                        try
                        {
                            await Clients.Client(connectionId).SendAsync("RecallMessage", messageDto);
                        }
                        catch (Exception ex)
                        {
                            // 记录日志
                            // _logger.LogError(ex, "撤回消息失败，ConnectionId: {ConnectionId}", connectionId);
                        }
                    });
                }
                #endregion


                #region 通知自己其他在线设备
                await Parallel.ForEachAsync(otherPlatformConnections.Select(r => r.ConnectionId), parallelOptions, async (connectionId, cancellationToken) =>
                {
                    try
                    {
                        await Clients.Client(connectionId).SendAsync("RecallMessage", messageDto);
                    }
                    catch (Exception ex)
                    {
                        // 记录日志
                        // _logger.LogError(ex, "撤回消息失败，ConnectionId: {ConnectionId}", connectionId);
                    }
                });
                #endregion
            }
        }

        [Authorize]
        [HubMethodName("RequestContact")]
        public async Task OnRequestContactNotice(long contactId, RequestContactReturn result)
        {
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = 20,
                CancellationToken = CancellationToken.None
            };

            var contactConnectionIds = GetAllPlatformConnectionIds(contactId);

            #region 通知对方所有在线设备
            var sendResults = new ConcurrentBag<bool>();

            await Parallel.ForEachAsync(contactConnectionIds, parallelOptions, async (connectionId, cancellationToken) =>
            {
                try
                {
                    var success = await Clients.Client(connectionId).InvokeAsync<bool>("RequestContact", result.contactRequest, result.receiverContact, result.receiverChat, result.receiverMessage, CancellationToken.None);

                    sendResults.Add(success);
                }
                catch (Exception ex)
                {
                    // 记录日志
                    // _logger.LogError(ex, "请求联系人失败，ConnectionId: {ConnectionId}", connectionId);
                }
            });

            // 至少有一个接收成功，更新未读数
            if (sendResults.Any(x => x))
            {
                await _chatService.ClearChatUnread(result.receiverMessage!.Sender_Id, result.receiverMessage!.Target_Id, ChatType.Private);
            }
            #endregion
        }

        [Authorize]
        [HubMethodName("RespondContact")]
        public async Task OnRespondContactNotice(RespondContactHandle handle, RespondContactReturn result)
        {
            long contactId = 0;
            ContactRequestDto? contactRequest = null;
            ContactDto? senderContact = null;
            ChatDto? senderChat = null;
            MessageDto? senderMessage = null;

            if (handle == RespondContactHandle.Revoked)
            {
                contactId = result.contactRequest.Receiver_Id;
                contactRequest = result.contactRequest;
            }
            else if (handle == RespondContactHandle.Declined)
            {
                contactId = result.contactRequest.Sender_Id;
                contactRequest = result.contactRequest;
            }
            else if (handle == RespondContactHandle.Accepted)
            {
                contactId = result.contactRequest.Sender_Id;
                contactRequest = result.contactRequest;
                senderContact = result.senderContact;
                senderChat = result.senderChat;
                senderMessage = result.senderMessage;
            }

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = 20,
                CancellationToken = CancellationToken.None
            };

            var contactConnectionIds = GetAllPlatformConnectionIds(contactId);
            await Parallel.ForEachAsync(contactConnectionIds, parallelOptions, async (connectionId, cancellationToken) =>
            {
                try
                {
                    await Clients.Client(connectionId).SendAsync("RespondContact", contactRequest, senderContact, senderChat, senderMessage);
                }
                catch (Exception ex)
                {
                    // 记录日志
                    // _logger.LogError(ex, "回复联系人失败，ConnectionId: {ConnectionId}", connectionId);
                }
            });
        }


    }
}
