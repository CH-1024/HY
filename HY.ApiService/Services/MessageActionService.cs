using HY.ApiService.Entities;
using HY.ApiService.Enums;
using HY.ApiService.Repositories;

namespace HY.ApiService.Services
{
    public interface IMessageActionService
    {
        Task<bool> InsertMessageAction(long userId, long messageId, MessageActionType actiontype);

    }

    public class MessageActionService : IMessageActionService
    {
        private readonly IUserRepository _userRepository;
        private readonly IChatRepository _chatRepository;
        private readonly IMessageRepository _messageRepository;
        readonly IMessageActionRepository _messageActionRepository;

        public MessageActionService(IUserRepository userRepository, IChatRepository chatRepository, IMessageRepository messageRepository, IMessageActionRepository messageActionRepository)
        {
            _userRepository = userRepository;
            _chatRepository = chatRepository;
            _messageRepository = messageRepository;
            _messageActionRepository = messageActionRepository;
        }

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
    }
}
