using HY.ApiService.Dtos;
using HY.ApiService.Entities;
using HY.ApiService.Enums;
using HY.ApiService.Repositories;
using HY.ApiService.Tools;
using Mapster;
using SqlSugar;
using System.Net.NetworkInformation;

namespace HY.ApiService.Services
{
    public interface IMessageService
    {
        // MessageAction
        Task<bool> InsertMessageAction(long userId, long messageId, MessageActionType actiontype);


        // Message
        Task<bool> HandleNewMessage(MessageDto messageDto);

        Task<long> InsertMessage(MessageDto messageDto);

        Task<MessageDto?> GetMessageById(long currentUserId, long messageId);
        Task<List<MessageDto>> GetMessagesByChatId(long chatId, long skipMessageId, int take);
        Task<List<MessageDto>> GetPrivateChatMessages(long currentUserId, long targetUserId, long skipMessageId, int take);
        Task<List<MessageDto>> GetGroupChatMessages(long currentUserId, long groupId, long skipMessageId, int take);

        Task<bool> RecallMessage(long messageId);
    }

    public class MessageService : IMessageService
    {
        private readonly ISqlSugarClient _db;

        private readonly IUserRepository _userRepository;
        private readonly IChatRepository _chatRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly IMessageActionRepository _messageActionRepository;
        private readonly IGroupMemberRepository _groupMemberRepository;

        public MessageService(ISqlSugarClient db, IUserRepository userRepository, IChatRepository chatRepository, IMessageRepository messageRepository, IMessageActionRepository messageActionRepository, IGroupMemberRepository groupMemberRepository)
        {
            _db = db;
            _userRepository = userRepository;
            _chatRepository = chatRepository;
            _messageRepository = messageRepository;
            _messageActionRepository = messageActionRepository;
            _groupMemberRepository = groupMemberRepository;
        }


        // MessageAction
        public async Task<bool> InsertMessageAction(long userId, long messageId, MessageActionType actiontype)
        {
            var messageActionEntity = new MessageActionEntity
            {
                User_Id = userId,
                Message_Id = messageId,
                Action_Type = actiontype,
                Created_At = DateTime.UtcNow,
            };
            return await _messageActionRepository.InsertMessageAction(messageActionEntity);
        }



        // Message
        public async Task<bool> HandleNewMessage(MessageDto messageDto)
        {
            // 开启事务
            var result = await _db.Ado.UseTranAsync(async () =>
            {
                // 保存消息
                var messageEntity = messageDto.Adapt<MessageEntity>();
                messageDto.Id = await _messageRepository.InsertMessage(messageEntity);
                if (messageDto.Id == 0) throw new Exception("保存消息失败");

                // 更新会话的最后一条消息
                if (messageDto.Chat_Type == ChatType.Private)
                {
                    // 单人

                    var senderChatEntity = await _chatRepository.GetChatByUserIdAndType(messageDto.Sender_Id, messageDto.Target_Id, ChatType.Private);
                    if (senderChatEntity != null)
                    {
                        senderChatEntity.Is_Deleted = false;
                        senderChatEntity.Last_Msg_Id = messageDto.Id;
                        //senderChatEntity.Unread_Count = 0;
                        senderChatEntity.Last_Msg_Time = messageDto.Created_At;
                        var bol = await _chatRepository.UpdateChat(senderChatEntity);
                        if (!bol) throw new Exception("更新发送者聊天记录失败");
                    }

                    var receiverChatEntity = await _chatRepository.GetChatByUserIdAndType(messageDto.Target_Id, messageDto.Sender_Id, ChatType.Private);
                    if (receiverChatEntity != null)
                    {
                        receiverChatEntity.Is_Deleted = false;
                        receiverChatEntity.Last_Msg_Id = messageDto.Id;
                        receiverChatEntity.Unread_Count += 1;
                        receiverChatEntity.Last_Msg_Time = messageDto.Created_At;
                        var bol = await _chatRepository.UpdateChat(receiverChatEntity);
                        if (!bol) throw new Exception("更新接收者聊天记录失败");
                    }
                }
                else if (messageDto.Chat_Type == ChatType.Group)
                {
                    // 群聊

                    var groupMembers = await _groupMemberRepository.GetGroupMembersByGroupId(messageDto.Target_Id);

                    var userIds = groupMembers.Select(m => m.User_Id).ToList();

                    var memberChatEntities = await _chatRepository.GetChatsByUserIdsAndType(userIds, messageDto.Target_Id, ChatType.Group);
                    foreach (var memberChatEntity in memberChatEntities)
                    {
                        if (memberChatEntity.User_Id == messageDto.Sender_Id)
                        {
                            memberChatEntity.Is_Deleted = false;
                            memberChatEntity.Last_Msg_Id = messageDto.Id;
                            //memberChatEntity.Unread_Count = 0;
                            memberChatEntity.Last_Msg_Time = messageDto.Created_At;
                        }
                        else
                        {
                            memberChatEntity.Is_Deleted = false;
                            memberChatEntity.Last_Msg_Id = messageDto.Id;
                            memberChatEntity.Unread_Count += 1;
                            memberChatEntity.Last_Msg_Time = messageDto.Created_At;
                        }
                    }

                    var bol = await _chatRepository.UpdateChats(memberChatEntities);
                    if (!bol) throw new Exception("更新群成员聊天记录失败");
                }
            });

            // ---------- 事务结束 ----------
            if (result.IsSuccess)
            {
                // 设置消息状态和创建时间
                messageDto.Message_Status = MessageStatus.Sented;
                messageDto.Created_At = DateTime.UtcNow;
            }

            return result.IsSuccess;
        }


        public async Task<long> InsertMessage(MessageDto messageDto)
        {
            var messageEntity = messageDto.Adapt<MessageEntity>();
            messageDto.Id = await _messageRepository.InsertMessage(messageEntity);
            return messageDto.Id;
        }



        public async Task<MessageDto?> GetMessageById(long currentUserId, long messageId)
        {
            var messageEntity = await _messageRepository.GetMessageById(currentUserId, messageId);
            if (messageEntity == null)
            {
                return null;
            }
            var sender = await _userRepository.GetUserById(messageEntity.Sender_Id);

            var messageDto = messageEntity.Adapt<MessageDto>();
            messageDto.Sender_Avatar = sender?.Avatar;
            messageDto.Sender_Nickname = sender?.Nickname;

            return messageDto;
        }

        public async Task<List<MessageDto>> GetMessagesByChatId(long chatId, long skipMessageId, int take)
        {
            var chatEntity = await _chatRepository.GetChatByChatId(chatId);

            if (chatEntity?.Type == ChatType.Private)
            {
                return await GetPrivateChatMessages(chatEntity.User_Id, chatEntity.Target_Id, skipMessageId, take);
            }
            else if (chatEntity?.Type == ChatType.Group)
            {
                return await GetGroupChatMessages(chatEntity.User_Id, chatEntity.Target_Id, skipMessageId, take);
            }
            else
            {
                return new List<MessageDto>();
            }
        }

        public async Task<List<MessageDto>> GetPrivateChatMessages(long currentUserId, long targetUserId, long skipMessageId, int take)
        {
            var messageEntities = await _messageRepository.GetPrivateChatMessages(currentUserId, targetUserId, skipMessageId, take);

            var senderIds = messageEntities.Select(m => m.Sender_Id).Distinct().ToList();

            var senderMap = (await _userRepository.GetUsersByIds(senderIds)).ToDictionary(x => x.Id);

            var messageDtos = messageEntities.Adapt<List<MessageDto>>();

            foreach (var messageDto in messageDtos)
            {
                if (senderMap.TryGetValue(messageDto.Sender_Id, out var sender))
                {
                    messageDto.Sender_Avatar = sender.Avatar;
                    messageDto.Sender_Nickname = sender.Nickname;
                }
            }

            return messageDtos;
        }

        public async Task<List<MessageDto>> GetGroupChatMessages(long currentUserId, long groupId, long skipMessageId, int take)
        {
            var messageEntities = await _messageRepository.GetGroupChatMessages(currentUserId, groupId, skipMessageId, take);

            var senderIds = messageEntities.Select(m => m.Sender_Id).Distinct().ToList();

            var senderMap = (await _userRepository.GetUsersByIds(senderIds)).ToDictionary(x => x.Id);

            var messageDtos = messageEntities.Adapt<List<MessageDto>>();

            foreach (var messageDto in messageDtos)
            {
                if (senderMap.TryGetValue(messageDto.Sender_Id, out var sender))
                {
                    messageDto.Sender_Avatar = sender.Avatar;
                    messageDto.Sender_Nickname = sender.Nickname;
                }
            }

            return messageDtos;
        }



        public async Task<bool> RecallMessage(long messageId)
        {
            return await _messageRepository.RecallMessage(messageId);
        }
    }
}
