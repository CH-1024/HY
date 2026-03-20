using HY.ApiService.Dtos;
using HY.ApiService.Entities;
using HY.ApiService.Enums;
using HY.ApiService.Repositories;
using HY.ApiService.Tools;
using Mapster;
using System.Net.NetworkInformation;

namespace HY.ApiService.Services
{
    public interface IMessageService
    {
        Task<long> InsertMessage(MessageDto messageDto);

        Task<MessageDto?> GetMessageById(long currentUserId, long messageId);
        Task<List<MessageDto>> GetMessagesByChatId(long chatId, long skipMessageId, int take);
        Task<List<MessageDto>> GetPrivateChatMessages(long currentUserId, long targetUserId, long skipMessageId, int take);
        Task<List<MessageDto>> GetGroupChatMessages(long currentUserId, long groupId, long skipMessageId, int take);

        Task<bool> RecallMessage(long messageId);
    }

    public class MessageService : IMessageService
    {
        private readonly IUserRepository _userRepository;
        private readonly IChatRepository _chatRepository;
        private readonly IMessageRepository _messageRepository;

        public MessageService(IUserRepository userRepository, IChatRepository chatRepository, IMessageRepository messageRepository)
        {
            _userRepository = userRepository;
            _chatRepository = chatRepository;
            _messageRepository = messageRepository;
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
