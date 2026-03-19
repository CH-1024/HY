using HY.ApiService.Dtos;
using HY.ApiService.Entities;
using HY.ApiService.Enums;
using HY.ApiService.Repositories;
using Mapster;

namespace HY.ApiService.Services
{
    public interface IChatService
    {
        Task<List<ChatDto>> GetChatsByUserId(long userId);

        Task UpdateChatLastMessage(MessageDto messageDto);
        Task<bool> UpdateChatUnread(long chat_Id);
        Task<bool> UpdateChatUnread(long userId, long targetId, ChatType chatType);
    }


    public class ChatService : IChatService
    {
        private readonly IChatRepository _chatRepository;
        private readonly IUserRepository _userRepository;
        private readonly IGroupRepository _groupRepository;
        private readonly IGroupMemberRepository _groupMemberRepository;
        private readonly IMessageRepository _messageRepository;



        public ChatService(IChatRepository chatRepository, IUserRepository userRepository, IGroupRepository groupRepository, IGroupMemberRepository groupMemberRepository, IMessageRepository messageRepository)
        {
            _chatRepository = chatRepository;
            _userRepository = userRepository;
            _groupRepository = groupRepository;
            _groupMemberRepository = groupMemberRepository;
            _messageRepository = messageRepository;
        }



        public async Task<List<ChatDto>> GetChatsByUserId(long userId)
        {
            var chatEntities = await _chatRepository.GetChatsByUserId(userId);

            var userIds = chatEntities.Where(c => c.Type == ChatType.Private).Select(c => c.Target_Id).Distinct().ToList();
            var groupIds = chatEntities.Where(c => c.Type == ChatType.Group).Select(c => c.Target_Id).Distinct().ToList();
            var lastMsgIds = chatEntities.Select(c => c.Last_Msg_Id).Where(i => i > 0).Distinct().ToList();

            var userMap = (await _userRepository.GetUsersByIds(userIds)).ToDictionary(x => x.Id);
            var groupMap = (await _groupRepository.GetGroupsByIds(groupIds)).ToDictionary(x => x.Id);
            var lastMsgMap = (await _messageRepository.GetMessagesByIds(userId, lastMsgIds)).ToDictionary(x => x.Id);

            var chatDtos = new List<ChatDto>();
            foreach (var chatEntity in chatEntities)
            {
                var chatDto = chatEntity.Adapt<ChatDto>();
                if (chatDto.Type == ChatType.Private && userMap.TryGetValue(chatDto.Target_Id, out var user)) // 单聊
                {
                    chatDto.Target_Name = user.Nickname;
                    chatDto.Target_Avatar = user.Avatar;
                }
                else if (chatDto.Type == ChatType.Group && groupMap.TryGetValue(chatDto.Target_Id, out var group)) // 群聊
                {
                    chatDto.Target_Name = group.Name;
                    chatDto.Target_Avatar = group.Avatar;
                }

                if (chatEntity.Last_Msg_Id == 0)
                {
                    // 没有Last_Msg
                }
                else if (lastMsgMap.TryGetValue(chatEntity.Last_Msg_Id, out var lastMsg))
                {
                    // 存在未删除Last_Msg
                    chatDto.Last_Msg_Type = lastMsg.Message_Type;
                    chatDto.Last_Msg_Brief = lastMsg.Content?.Length > 20 ? lastMsg.Content.Substring(0, 20) + "..." : lastMsg.Content;
                    chatDto.Last_Msg_Status = lastMsg.Message_Status;
                }
                else
                {
                    // 已删除Last_Msg
                    chatDto.Last_Msg_Brief = null;
                    chatDto.Last_Msg_Status = MessageStatus.Deleted;
                }

                chatDtos.Add(chatDto);
            }

            return chatDtos;
        }





        public async Task UpdateChatLastMessage(MessageDto messageDto)
        {
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
                    await _chatRepository.UpdateChat(senderChatEntity);
                }

                var receiverChatEntity = await _chatRepository.GetChatByUserIdAndType(messageDto.Target_Id, messageDto.Sender_Id, ChatType.Private);
                if (receiverChatEntity != null)
                {
                    receiverChatEntity.Is_Deleted = false;
                    receiverChatEntity.Last_Msg_Id = messageDto.Id;
                    receiverChatEntity.Unread_Count += 1;
                    receiverChatEntity.Last_Msg_Time = messageDto.Created_At;
                    await _chatRepository.UpdateChat(receiverChatEntity);
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

                await _chatRepository.UpdateChats(memberChatEntities);
            }
        }

        public async Task<bool> UpdateChatUnread(long chat_Id)
        {
            return await _chatRepository.UpdateChatUnread(chat_Id);
        }

        public async Task<bool> UpdateChatUnread(long userId, long targetId, ChatType chatType)
        {
            var chatEntity = await _chatRepository.GetChatByUserIdAndType(userId, targetId, chatType);
            if (chatEntity != null)
            {
                return await _chatRepository.UpdateChatUnread(chatEntity.Id);
            }
            return false;
        }
    }
}
