using HY.ApiService.Dtos;
using HY.ApiService.Enums;
using HY.ApiService.Hubs;
using HY.ApiService.Hubs.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace HY.ApiService.Services
{
    public interface IChatNotificationService
    {
        Task SendMessageNotify(MessageDto messageDto, int platform);
        Task RecallMessageNotify(MessageDto messageDto, int platform);

        Task CreateCallNotify(CallDto callDto);
        Task CancelCallNotify(CallDto callDto);
        Task<bool> AcceptCallNotify(CallDto callDto);
        Task RejectCallNotify(CallDto callDto);
        Task AbnormalCallNotify(ChatType chatType, string callId, long userId, int userPlatform);
        Task HangUpCallNotify(ChatType chatType, string callId, long userId, int userPlatform);

        Task RequestContactNotify(long contactId, RequestContactReturn result);
        Task RespondContactNotify(RespondContactHandle handle, RespondContactReturn result);
    }


    public class ChatNotificationService : IChatNotificationService
    {
        readonly IHubContext<ChatHub> _chatHub;
        readonly IRedisConnectionService _redisConnectionService;

        readonly IChatService _chatService;
        readonly IContactService _contactService;
        readonly IGroupMemberService _groupMemberService;


        public ChatNotificationService(IHubContext<ChatHub> chatHub, IRedisConnectionService redisConnectionService, IChatService chatService, IContactService contactService, IGroupMemberService groupMemberService)
        {
            _chatHub = chatHub;
            _redisConnectionService = redisConnectionService;

            _chatService = chatService;
            _contactService = contactService;
            _groupMemberService = groupMemberService;
        }



        public async Task SendMessageNotify(MessageDto messageDto, int platform)
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

                var receiverConnectionIds = await _redisConnectionService.GetAllPlatformConnectionIdsAsync(messageDto.Target_Id);
                var otherPlatformConnectionIds = await _redisConnectionService.GetOtherPlatformConnectionIdsAsync(messageDto.Sender_Id, platform);

                #region 通知对方所有在线设备
                var sendResults = new ConcurrentBag<bool>();

                await Parallel.ForEachAsync(receiverConnectionIds, parallelOptions, async (connectionId, cancellationToken) =>
                {
                    try
                    {
                        var success = await _chatHub.Clients.Client(connectionId).InvokeAsync<bool>("ReceiveMessage", messageDto, cancellationToken);

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
                        await _chatHub.Clients.Client(connectionId).InvokeAsync<bool>("ReceiveMessage", messageDto, cancellationToken);
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
                        var connectionIds = await _redisConnectionService.GetOtherPlatformConnectionIdsAsync(member.User_Id, platform);
                        otherPlatformConnections.AddRange(connectionIds.Select(connId => (UserId: member.User_Id, ConnectionId: connId)));
                    }
                    else
                    {
                        // 成员所有在线设备
                        var connectionIds = await _redisConnectionService.GetAllPlatformConnectionIdsAsync(member.User_Id);
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
                            var success = await _chatHub.Clients.Client(connectionId).InvokeAsync<bool>("ReceiveMessage", messageDto, cancellationToken);

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
                        await _chatHub.Clients.Client(connectionId).InvokeAsync<bool>("ReceiveMessage", messageDto, cancellationToken);
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

        public async Task RecallMessageNotify(MessageDto messageDto, int platform)
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

                var receiverConnectionIds = await _redisConnectionService.GetAllPlatformConnectionIdsAsync(messageDto.Target_Id);
                var otherPlatformConnectionIds = await _redisConnectionService.GetOtherPlatformConnectionIdsAsync(messageDto.Sender_Id, platform);

                #region 通知对方所有在线设备
                await Parallel.ForEachAsync(receiverConnectionIds, parallelOptions, async (connectionId, cancellationToken) =>
                {
                    try
                    {
                        await _chatHub.Clients.Client(connectionId).SendAsync("RecallMessage", messageDto);
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
                        await _chatHub.Clients.Client(connectionId).SendAsync("RecallMessage", messageDto);
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
                        var connectionIds = await _redisConnectionService.GetOtherPlatformConnectionIdsAsync(member.User_Id, platform);
                        otherPlatformConnections.AddRange(connectionIds.Select(connId => (UserId: member.User_Id, ConnectionId: connId)));
                    }
                    else
                    {
                        // 成员所有在线设备
                        var connectionIds = await _redisConnectionService.GetAllPlatformConnectionIdsAsync(member.User_Id);
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
                            await _chatHub.Clients.Client(connectionId).SendAsync("RecallMessage", messageDto);
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
                        await _chatHub.Clients.Client(connectionId).SendAsync("RecallMessage", messageDto);
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


        public async Task CreateCallNotify(CallDto callDto)
        {
            var callId = callDto.CallId;
            var callType = callDto.CallType;
            var chatType = callDto.ChatType;
            var callerId = callDto.CallerId;
            var callerPlatform = callDto.CallerPlatform;
            var calleeId = callDto.CalleeId;
            var expiry = callDto.ExpiryAt;

            if (chatType == ChatType.Private)
            {
                // 私聊

                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 20,
                    CancellationToken = CancellationToken.None
                };

                var calleeConnectionIds = await _redisConnectionService.GetAllPlatformConnectionIdsAsync(calleeId);

                #region 通知被呼叫方所有在线设备
                var rcr = new ReceiveCallRequest()
                {
                    CallId = callId,
                    CallType = callType,
                    ChatType = chatType,
                    CallerId = callerId,
                    Expiry = expiry,
                };
                await Parallel.ForEachAsync(calleeConnectionIds, parallelOptions, async (connectionId, cancellationToken) =>
                {
                    try
                    {
                        await _chatHub.Clients.Client(connectionId).SendAsync("ReceiveCall", rcr, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        // 记录日志
                        // _logger.LogError(ex, "[CreateCallNotification] : ConnectionId: {ConnectionId}", connectionId);
                    }
                });
                #endregion

            }
            else if (chatType == ChatType.Group)
            {
                // 群聊

            }
        }

        public async Task CancelCallNotify(CallDto callDto)
        {
            var callId = callDto.CallId;
            var callType = callDto.CallType;
            var chatType = callDto.ChatType;
            var callerId = callDto.CallerId;
            var callerPlatform = callDto.CallerPlatform;
            var calleeId = callDto.CalleeId;
            var expiry = callDto.ExpiryAt;

            if (chatType == ChatType.Private)
            {
                // 私聊

                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 20,
                    CancellationToken = CancellationToken.None
                };

                var calleeConnectionIds = await _redisConnectionService.GetAllPlatformConnectionIdsAsync(calleeId);

                #region 通知被呼叫方所有在线设备
                await Parallel.ForEachAsync(calleeConnectionIds, parallelOptions, async (connectionId, cancellationToken) =>
                {
                    try
                    {
                        await _chatHub.Clients.Client(connectionId).SendAsync("CallCanceled", callId);
                    }
                    catch (Exception ex)
                    {
                        // 记录日志
                        // _logger.LogError(ex, "[CancelCallNotification] : ConnectionId: {ConnectionId}", connectionId);
                    }
                });
                #endregion

            }
            else if (chatType == ChatType.Group)
            {
                // 群聊

            }
        }

        public async Task<bool> AcceptCallNotify(CallDto callDto)
        {
            var callId = callDto.CallId;
            var callType = callDto.CallType;
            var chatType = callDto.ChatType;
            var callerId = callDto.CallerId;
            var callerPlatform = callDto.CallerPlatform;
            var calleeId = callDto.CalleeId;
            var calleePlatform = callDto.CalleePlatform;
            var expiry = callDto.ExpiryAt;

            if (chatType == ChatType.Private)
            {
                // 私聊

                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 20,
                    CancellationToken = CancellationToken.None
                };

                var callerConnectionId = await _redisConnectionService.GetConnectionIdAsync(callerId, callerPlatform);
                var calleeConnectionIds = await _redisConnectionService.GetOtherPlatformConnectionIdsAsync(calleeId, calleePlatform);

                #region 通知呼叫方在线设备
                var acceptResult = await _chatHub.Clients.Client(callerConnectionId!).InvokeAsync<bool>("CallAccepted", callId, parallelOptions.CancellationToken);
                #endregion


                #region 通知被呼叫方其他在线设备
                await Parallel.ForEachAsync(calleeConnectionIds, parallelOptions, async (connectionId, cancellationToken) =>
                {
                    try
                    {
                        await _chatHub.Clients.Client(connectionId).SendAsync("CallHandled", callId, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        // 记录日志
                        // _logger.LogError(ex, "[AcceptCallNotification] : ConnectionId: {ConnectionId}", connectionId);
                    }
                });
                #endregion

                return acceptResult;
            }
            else if (chatType == ChatType.Group)
            {
                // 群聊

            }

            return false;
        }

        public async Task RejectCallNotify(CallDto callDto)
        {
            var callId = callDto.CallId;
            var callType = callDto.CallType;
            var chatType = callDto.ChatType;
            var callerId = callDto.CallerId;
            var callerPlatform = callDto.CallerPlatform;
            var calleeId = callDto.CalleeId;
            var calleePlatform = callDto.CalleePlatform;
            var expiry = callDto.ExpiryAt;

            if (chatType == ChatType.Private)
            {
                // 私聊

                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 20,
                    CancellationToken = CancellationToken.None
                };

                var callerConnectionId = await _redisConnectionService.GetConnectionIdAsync(callerId, callerPlatform);
                var calleeConnectionIds = await _redisConnectionService.GetOtherPlatformConnectionIdsAsync(calleeId, calleePlatform);

                #region 通知呼叫方在线设备
                await _chatHub.Clients.Client(callerConnectionId!).SendAsync("CallRejected", callId, parallelOptions.CancellationToken);
                #endregion


                #region 通知被呼叫方其他在线设备
                await Parallel.ForEachAsync(calleeConnectionIds, parallelOptions, async (connectionId, cancellationToken) =>
                {
                    try
                    {
                        await _chatHub.Clients.Client(connectionId).SendAsync("CallHandled", callId, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        // 记录日志
                        // _logger.LogError(ex, "[RejectCallNotification] : ConnectionId: {ConnectionId}", connectionId);
                    }
                });
                #endregion

            }
            else if (chatType == ChatType.Group)
            {
                // 群聊

            }
        }

        public async Task AbnormalCallNotify(ChatType chatType, string callId, long userId, int userPlatform)
        {
            if (chatType == ChatType.Private)
            {
                // 私聊

                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 20,
                    CancellationToken = CancellationToken.None
                };

                var userConnectionId = await _redisConnectionService.GetConnectionIdAsync(userId, userPlatform);

                #region 通知对方在线设备
                await _chatHub.Clients.Client(userConnectionId!).SendAsync("CallAbnormal", callId, parallelOptions.CancellationToken);
                #endregion
            }
            else if (chatType == ChatType.Group)
            {
                // 群聊

            }
        }

        public async Task HangUpCallNotify(ChatType chatType, string callId, long userId, int userPlatform)
        {
            if (chatType == ChatType.Private)
            {
                // 私聊

                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 20,
                    CancellationToken = CancellationToken.None
                };

                var userConnectionId = await _redisConnectionService.GetConnectionIdAsync(userId, userPlatform);

                #region 通知对方在线设备
                await _chatHub.Clients.Client(userConnectionId!).SendAsync("CallHangUp", callId, parallelOptions.CancellationToken);
                #endregion
            }
            else if (chatType == ChatType.Group)
            {
                // 群聊

            }
        }


        public async Task RequestContactNotify(long contactId, RequestContactReturn result)
        {
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = 20,
                CancellationToken = CancellationToken.None
            };

            var contactConnectionIds = await _redisConnectionService.GetAllPlatformConnectionIdsAsync(contactId);

            #region 通知对方所有在线设备
            var sendResults = new ConcurrentBag<bool>();

            await Parallel.ForEachAsync(contactConnectionIds, parallelOptions, async (connectionId, cancellationToken) =>
            {
                try
                {
                    var success = await _chatHub.Clients.Client(connectionId).InvokeAsync<bool>("RequestContact", result.contactRequest, result.receiverContact, result.receiverChat, result.receiverMessage, CancellationToken.None);

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

        public async Task RespondContactNotify(RespondContactHandle handle, RespondContactReturn result)
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

            var contactConnectionIds = await _redisConnectionService.GetAllPlatformConnectionIdsAsync(contactId);
            await Parallel.ForEachAsync(contactConnectionIds, parallelOptions, async (connectionId, cancellationToken) =>
            {
                try
                {
                    await _chatHub.Clients.Client(connectionId).SendAsync("RespondContact", contactRequest, senderContact, senderChat, senderMessage);
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
