using HY.ApiService.Dtos;
using HY.ApiService.Enums;
using HY.ApiService.Hubs;
using HY.ApiService.Hubs.Requests;
using HY.ApiService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace HY.ApiService.Services
{
    public interface IChatNotificationService
    {
        Task SendMessageNotify(MessageDto messageDto, int platform);
        Task RecallMessageNotify(MessageDto messageDto, int platform);

        Task CreateCallNotify(CallInfo callDto);
        Task CancelCallNotify(CallInfo callDto);
        Task<bool> AcceptCallNotify(CallInfo callDto);
        Task RejectCallNotify(CallInfo callDto);
        Task AbnormalCallNotify(CallInfo callDto, long currentUserId);
        Task HangUpCallNotify(CallInfo callDto, long currentUserId);
        Task ExpiryCallNotify(CallInfo callDto);

        Task RequestContactNotify(long contactId, RequestContactReturn result);
        Task RespondContactNotify(RespondContactHandle handle, RespondContactReturn result);
    }


    public class ChatNotificationService : IChatNotificationService
    {
        readonly IHubContext<ChatHub> _chatHub;
        readonly IRedisConnectionService _redisConnectionService;

        readonly IChatService _chatService;
        readonly IUserService _userService;
        readonly IMessageService _messageService;
        readonly IContactService _contactService;
        readonly IGroupMemberService _groupMemberService;


        public ChatNotificationService(IHubContext<ChatHub> chatHub, IRedisConnectionService redisConnectionService, IChatService chatService, IUserService userService, IMessageService messageService, IContactService contactService, IGroupMemberService groupMemberService)
        {
            _chatHub = chatHub;
            _redisConnectionService = redisConnectionService;

            _chatService = chatService;
            _userService = userService;
            _messageService = messageService;
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


                #region 通知对方所有在线设备
                var hasSuccess = 0;
                var receiverConnectionIds = await _redisConnectionService.GetAllPlatformConnectionIdsAsync(messageDto.Target_Id);
                await Parallel.ForEachAsync(receiverConnectionIds, parallelOptions, async (connectionId, cancellationToken) =>
                {
                    try
                    {
                        var success = await _chatHub.Clients.Client(connectionId).InvokeAsync<bool>("ReceiveMessage", messageDto, cancellationToken);
                        if (success) Interlocked.Exchange(ref hasSuccess, 1);
                    }
                    catch (Exception ex)
                    {
                        // 记录日志
                        // _logger.LogError(ex, "发送消息失败，ConnectionId: {ConnectionId}", connectionId);
                    }
                });

                // 接收成功，更新未读数
                if (hasSuccess == 1)
                {
                    await _chatService.ClearChatUnread(messageDto.Target_Id, messageDto.Sender_Id, ChatType.Private);
                }
                #endregion


                #region 通知自己其他在线设备
                var otherPlatformConnectionIds = await _redisConnectionService.GetOtherPlatformConnectionIdsAsync(messageDto.Sender_Id, platform);
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
                    var hasSuccess = 0;

                    await Parallel.ForEachAsync(group.Select(r => r.ConnectionId), parallelOptions, async (connectionId, cancellationToken) =>
                    {
                        try
                        {
                            var success = await _chatHub.Clients.Client(connectionId).InvokeAsync<bool>("ReceiveMessage", messageDto, cancellationToken);
                            if (success) Interlocked.Exchange(ref hasSuccess, 1);
                        }
                        catch (Exception ex)
                        {
                            // 记录日志
                            // _logger.LogError(ex, "发送消息失败，ConnectionId: {ConnectionId}", connectionId);
                        }
                    });

                    // 接收成功，更新未读数
                    if (hasSuccess == 1)
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


                #region 通知对方所有在线设备
                var receiverConnectionIds = await _redisConnectionService.GetAllPlatformConnectionIdsAsync(messageDto.Target_Id);
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
                var otherPlatformConnectionIds = await _redisConnectionService.GetOtherPlatformConnectionIdsAsync(messageDto.Sender_Id, platform);
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



        public async Task CreateCallNotify(CallInfo callDto)
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
                var rcr = new ReceiveCallRequest()
                {
                    CallId = callId,
                    CallType = callType,
                    ChatType = chatType,
                    CallerId = callerId,
                };

                #region 通知被呼叫方所有在线设备
                var calleeConnectionIds = await _redisConnectionService.GetAllPlatformConnectionIdsAsync(calleeId);
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

        public async Task<bool> AcceptCallNotify(CallInfo callDto)
        {
            var callId = callDto.CallId;
            var callType = callDto.CallType;
            var chatType = callDto.ChatType;
            var callerId = callDto.CallerId;
            var callerPlatform = callDto.CallerPlatform;
            var calleeId = callDto.CalleeId;
            var calleePlatform = callDto.CalleePlatform;
            var expiry = callDto.ExpiryAt;
            var start = callDto.StartAt!.Value;

            if (chatType == ChatType.Private)
            {
                // 私聊

                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 20,
                    CancellationToken = CancellationToken.None
                };

                #region 通知呼叫方在线设备
                var callerConnectionId = await _redisConnectionService.GetConnectionIdAsync(callerId, callerPlatform);
                var acceptResult = await _chatHub.Clients.Client(callerConnectionId!).InvokeAsync<bool>("CallAccepted", callId, start, parallelOptions.CancellationToken);
                #endregion


                #region 通知被呼叫方其他在线设备
                var calleeConnectionIds = await _redisConnectionService.GetOtherPlatformConnectionIdsAsync(calleeId, calleePlatform);
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

        public async Task CancelCallNotify(CallInfo callDto)
        {
            var callId = callDto.CallId;
            var callType = callDto.CallType;
            var chatType = callDto.ChatType;
            var callerId = callDto.CallerId;
            var callerPlatform = callDto.CallerPlatform;
            var calleeId = callDto.CalleeId;
            var create = callDto.CreateAt;
            var expiry = callDto.ExpiryAt;

            if (chatType == ChatType.Private)
            {
                // 私聊

                #region 先发记录
                await SendMessageDto(new MessageDto
                {
                    Chat_Type = chatType,
                    Sender_Id = callerId,
                    Target_Id = calleeId,
                    Message_Type = callType == CallType.Video ? MessageType.VideoCall : MessageType.VoiceCall,
                    Content = null,
                    Extra = JsonSerializer.Serialize(new Dictionary<string, object?>
                    {
                        {"CallId", callId },
                        { "CallStatus", (int)CallStatus.Cancelled },
                        { "Duration", 0 }
                    }),
                    Message_Status = MessageStatus.Sented,
                    Created_At = DateTime.UtcNow
                });
                #endregion


                #region 在通知
                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 20,
                    CancellationToken = CancellationToken.None
                };

                #region 通知被呼叫方所有在线设备
                var calleeConnectionIds = await _redisConnectionService.GetAllPlatformConnectionIdsAsync(calleeId);
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
                #endregion

            }
            else if (chatType == ChatType.Group)
            {
                // 群聊

            }
        }

        public async Task RejectCallNotify(CallInfo callDto)
        {
            var callId = callDto.CallId;
            var callType = callDto.CallType;
            var chatType = callDto.ChatType;
            var callerId = callDto.CallerId;
            var callerPlatform = callDto.CallerPlatform;
            var calleeId = callDto.CalleeId;
            var calleePlatform = callDto.CalleePlatform;
            var create = callDto.CreateAt;
            var expiry = callDto.ExpiryAt;

            if (chatType == ChatType.Private)
            {
                // 私聊

                #region 先发记录
                await SendMessageDto(new MessageDto
                {
                    Chat_Type = chatType,
                    Sender_Id = callerId,
                    Target_Id = calleeId,
                    Message_Type = callType == CallType.Video ? MessageType.VideoCall : MessageType.VoiceCall,
                    Content = null,
                    Extra = JsonSerializer.Serialize(new Dictionary<string, object?>
                    {
                        {"CallId", callId },
                        { "CallStatus", (int)CallStatus.Rejected },
                        { "Duration", 0 }
                    }),
                    Message_Status = MessageStatus.Sented,
                    Created_At = DateTime.UtcNow
                });
                #endregion


                #region 在通知
                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 20,
                    CancellationToken = CancellationToken.None
                };

                #region 通知呼叫方在线设备
                var callerConnectionId = await _redisConnectionService.GetConnectionIdAsync(callerId, callerPlatform);
                await _chatHub.Clients.Client(callerConnectionId!).SendAsync("CallRejected", callId, parallelOptions.CancellationToken);
                #endregion


                #region 通知被呼叫方其他在线设备
                var calleeConnectionIds = await _redisConnectionService.GetOtherPlatformConnectionIdsAsync(calleeId, calleePlatform);
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

                #endregion
            }
            else if (chatType == ChatType.Group)
            {
                // 群聊

            }
        }

        public async Task AbnormalCallNotify(CallInfo callDto, long currentUserId)
        {
            var callId = callDto.CallId;
            var callType = callDto.CallType;
            var chatType = callDto.ChatType;
            var callerId = callDto.CallerId;
            var callerPlatform = callDto.CallerPlatform;
            var calleeId = callDto.CalleeId;
            var calleePlatform = callDto.CalleePlatform;
            var create = callDto.CreateAt;
            var expiry = callDto.ExpiryAt;
            var start = callDto.StartAt!.Value;

            if (chatType == ChatType.Private)
            {
                // 私聊

                #region 先发记录
                await SendMessageDto(new MessageDto
                {
                    Chat_Type = chatType,
                    Sender_Id = callerId,
                    Target_Id = calleeId,
                    Message_Type = callType == CallType.Video ? MessageType.VideoCall : MessageType.VoiceCall,
                    Content = null,
                    Extra = JsonSerializer.Serialize(new Dictionary<string, object?>
                    {
                        {"CallId", callId },
                        { "CallStatus", (int)CallStatus.Abnormal },
                        { "Duration", (DateTime.UtcNow - start).TotalSeconds }
                    }),
                    Message_Status = MessageStatus.Sented,
                    Created_At = DateTime.UtcNow
                });
                #endregion


                #region 在通知

                #region 通知对方在线设备
                var userId = currentUserId == callDto.CallerId ? callDto.CalleeId : callDto.CallerId;
                var userPlatform = currentUserId == callDto.CallerId ? callDto.CalleePlatform : callDto.CallerPlatform;

                var userConnectionId = await _redisConnectionService.GetConnectionIdAsync(userId, userPlatform);
                await _chatHub.Clients.Client(userConnectionId!).SendAsync("CallAbnormal", callId, CancellationToken.None);
                #endregion

                #endregion
            }
            else if (chatType == ChatType.Group)
            {
                // 群聊

            }
        }

        public async Task HangUpCallNotify(CallInfo callDto, long currentUserId)
        {
            var callId = callDto.CallId;
            var callType = callDto.CallType;
            var chatType = callDto.ChatType;
            var callerId = callDto.CallerId;
            var callerPlatform = callDto.CallerPlatform;
            var calleeId = callDto.CalleeId;
            var calleePlatform = callDto.CalleePlatform;
            var create = callDto.CreateAt;
            var expiry = callDto.ExpiryAt;
            var start = callDto.StartAt!.Value;

            if (chatType == ChatType.Private)
            {
                // 私聊

                #region 先发记录
                await SendMessageDto(new MessageDto
                {
                    Chat_Type = chatType,
                    Sender_Id = callerId,
                    Target_Id = calleeId,
                    Message_Type = callType == CallType.Video ? MessageType.VideoCall : MessageType.VoiceCall,
                    Content = null,
                    Extra = JsonSerializer.Serialize(new Dictionary<string, object?>
                    {
                        {"CallId", callId },
                        { "CallStatus", (int)CallStatus.Ended },
                        { "Duration", (DateTime.UtcNow - start).TotalSeconds }
                    }),
                    Message_Status = MessageStatus.Sented,
                    Created_At = DateTime.UtcNow
                });
                #endregion



                #region 在通知

                #region 通知呼叫方在线设备
                var callerConnectionId = await _redisConnectionService.GetConnectionIdAsync(callerId, callerPlatform);
                await _chatHub.Clients.Client(callerConnectionId!).SendAsync("CallHangUp", callId, CancellationToken.None);
                #endregion

                #region 通知被呼叫方在线设备
                var calleeConnectionId = await _redisConnectionService.GetConnectionIdAsync(calleeId, calleePlatform);
                await _chatHub.Clients.Client(calleeConnectionId!).SendAsync("CallHangUp", callId, CancellationToken.None);
                #endregion

                #endregion
            }
            else if (chatType == ChatType.Group)
            {
                // 群聊

            }
        }

        public async Task ExpiryCallNotify(CallInfo callDto)
        {
            var callId = callDto.CallId;
            var callType = callDto.CallType;
            var chatType = callDto.ChatType;
            var callerId = callDto.CallerId;
            var callerPlatform = callDto.CallerPlatform;
            var calleeId = callDto.CalleeId;
            var calleePlatform = callDto.CalleePlatform;
            var create = callDto.CreateAt;
            var expiry = callDto.ExpiryAt;

            if (chatType == ChatType.Private)
            {
                // 私聊

                #region 先发记录
                await SendMessageDto(new MessageDto
                {
                    Chat_Type = chatType,
                    Sender_Id = callerId,
                    Target_Id = calleeId,
                    Message_Type = callType == CallType.Video ? MessageType.VideoCall : MessageType.VoiceCall,
                    Content = null,
                    Extra = JsonSerializer.Serialize(new Dictionary<string, object?>
                    {
                        {"CallId", callId },
                        { "CallStatus", (int)CallStatus.Expired },
                        { "Duration", 0 }
                    }),
                    Message_Status = MessageStatus.Sented,
                    Created_At = DateTime.UtcNow
                });
                #endregion

                #region 在通知
                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 20,
                    CancellationToken = CancellationToken.None
                };


                #region 通知呼叫方在线设备
                var callerConnectionId = await _redisConnectionService.GetConnectionIdAsync(callerId, callerPlatform);
                await _chatHub.Clients.Client(callerConnectionId!).SendAsync("CallExpiry", callId, CancellationToken.None);
                #endregion


                #region 通知被呼叫方在线设备
                var calleeConnectionIds = await _redisConnectionService.GetAllPlatformConnectionIdsAsync(calleeId);
                await Parallel.ForEachAsync(calleeConnectionIds, parallelOptions, async (connectionId, cancellationToken) =>
                {
                    try
                    {
                        await _chatHub.Clients.Client(connectionId).SendAsync("CallExpiry", callId, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        // 记录日志
                        // _logger.LogError(ex, "[ExpiryCallNotification] : ConnectionId: {ConnectionId}", connectionId);
                    }
                });
                #endregion

                #endregion
            }
            else if (chatType == ChatType.Group)
            {
                // 群聊

            }
        }



        public async Task RequestContactNotify(long contactId, RequestContactReturn result)
        {
            var hasSuccess = 0;
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = 20,
                CancellationToken = CancellationToken.None
            };


            #region 通知对方所有在线设备

            var contactConnectionIds = await _redisConnectionService.GetAllPlatformConnectionIdsAsync(contactId);
            await Parallel.ForEachAsync(contactConnectionIds, parallelOptions, async (connectionId, cancellationToken) =>
            {
                try
                {
                    var success = await _chatHub.Clients.Client(connectionId).InvokeAsync<bool>("RequestContact", result.contactRequest, result.receiverContact, result.receiverChat, result.receiverMessage, CancellationToken.None);
                    if (success) Interlocked.Exchange(ref hasSuccess, 1);
                }
                catch (Exception ex)
                {
                    // 记录日志
                    // _logger.LogError(ex, "请求联系人失败，ConnectionId: {ConnectionId}", connectionId);
                }
            });

            // 接收成功，更新未读数
            if (hasSuccess == 1)
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










        private async Task SendMessageDto(MessageDto messageDto)
        {
            if (messageDto == null)
            {
                return;
            }

            var sender = await _userService.GetUserById(messageDto.Sender_Id);
            messageDto.Sender_Avatar = sender?.Avatar;
            messageDto.Sender_Nickname = sender?.Nickname;

            await _messageService.HandleNewMessage(messageDto);

            if (messageDto.Chat_Type == ChatType.Private)
            {
                // 单人

                var hasSuccess = 0;
                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 20,
                    CancellationToken = CancellationToken.None
                };


                #region 通知对方所有在线设备

                var receiverConnectionIds = await _redisConnectionService.GetAllPlatformConnectionIdsAsync(messageDto.Target_Id);
                await Parallel.ForEachAsync(receiverConnectionIds, parallelOptions, async (connectionId, cancellationToken) =>
                {
                    try
                    {
                        var success = await _chatHub.Clients.Client(connectionId).InvokeAsync<bool>("ReceiveMessage", messageDto, cancellationToken);
                        if (success) Interlocked.Exchange(ref hasSuccess, 1);
                    }
                    catch (Exception ex)
                    {
                        // 记录日志
                        // _logger.LogError(ex, "发送消息失败，ConnectionId: {ConnectionId}", connectionId);
                    }
                });

                // 接收成功，更新未读数
                if (hasSuccess == 1)
                {
                    await _chatService.ClearChatUnread(messageDto.Target_Id, messageDto.Sender_Id, ChatType.Private);
                }
                #endregion


                #region 通知自己所有在线设备
                hasSuccess = 0;

                var senderPlatformConnectionIds = await _redisConnectionService.GetAllPlatformConnectionIdsAsync(messageDto.Sender_Id);
                await Parallel.ForEachAsync(senderPlatformConnectionIds, parallelOptions, async (connectionId, cancellationToken) =>
                {
                    try
                    {
                        var success = await _chatHub.Clients.Client(connectionId).InvokeAsync<bool>("ReceiveMessage", messageDto, cancellationToken);
                        if (success) Interlocked.Exchange(ref hasSuccess, 1);
                    }
                    catch (Exception ex)
                    {
                        // 记录日志
                        // _logger.LogError(ex, "发送消息失败，ConnectionId: {ConnectionId}", connectionId);
                    }
                });

                // 接收成功，更新未读数
                if (hasSuccess == 1)
                {
                    await _chatService.ClearChatUnread(messageDto.Sender_Id, messageDto.Target_Id, ChatType.Private);
                }
                #endregion
            }
            else if (messageDto.Chat_Type == ChatType.Group)
            {
                // 群聊

            }

        }
    }
}
