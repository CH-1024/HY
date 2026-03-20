using HY.ApiService.Entities;
using HY.ApiService.Enums;
using SqlSugar;
using System.Collections;
using System.Net.NetworkInformation;

namespace HY.ApiService.Repositories
{
    public interface IMessageRepository
    {
        Task<long> InsertMessage(MessageEntity messageEntity);

        Task<MessageEntity> GetMessageById(long currentUserId, long messageId);
        Task<List<MessageEntity>> GetMessagesByIds(long currentUserId, List<long> msgIds);
        Task<List<MessageEntity>> GetPrivateChatMessages(long currentUserId, long targetUserId, long skipMessageId, int take);
        Task<List<MessageEntity>> GetGroupChatMessages(long currentUserId, long groupId, long skipMessageId, int take);

        Task<bool> RecallMessage(long messageId);
    }


    public class MessageRepository : IMessageRepository
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public MessageRepository(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }




        public async Task<long> InsertMessage(MessageEntity messageEntity)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            messageEntity.Id = await db.Insertable(messageEntity).ExecuteReturnBigIdentityAsync();
            return messageEntity.Id;
        }



        public async Task<MessageEntity> GetMessageById(long currentUserId, long messageId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            return await db.Queryable<MessageEntity>()
                .LeftJoin<MessageActionEntity>((m, a) => m.Id == a.Message_Id && a.User_Id == currentUserId && a.Action_Type == MessageActionType.Delete)
                .Where((m, a) => a.Message_Id == null)   // 过滤已删除
                .InSingleAsync(messageId);
        }

        public async Task<List<MessageEntity>> GetMessagesByIds(long currentUserId, List<long> msgIds)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            return await db.Queryable<MessageEntity>()
                .LeftJoin<MessageActionEntity>((m, a) => m.Id == a.Message_Id && a.User_Id == currentUserId && a.Action_Type == MessageActionType.Delete)
                .Where((m, a) => a.Message_Id == null)   // 过滤已删除
                .In(msgIds).ToListAsync();
        }

        public async Task<List<MessageEntity>> GetPrivateChatMessages(long currentUserId, long targetUserId, long skipMessageId, int take)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            return await db.Queryable<MessageEntity>()
                .LeftJoin<MessageActionEntity>((m, a) => m.Id == a.Message_Id && a.User_Id == currentUserId && a.Action_Type == MessageActionType.Delete)
                .Where((m, a) => a.Message_Id == null)   // 过滤已删除
                .Where((m, a) => ((m.Sender_Id == currentUserId && m.Target_Id == targetUserId) || (m.Sender_Id == targetUserId && m.Target_Id == currentUserId)) && m.Chat_Type == ChatType.Private)
                .WhereIF(skipMessageId > 0, (m, a) => m.Id < skipMessageId)
                .OrderBy((m, a) => m.Id, OrderByType.Desc)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<MessageEntity>> GetGroupChatMessages(long currentUserId, long groupId, long skipMessageId, int take)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            return await db.Queryable<MessageEntity>()
                .LeftJoin<MessageActionEntity>((m, a) => m.Id == a.Message_Id && a.User_Id == currentUserId && a.Action_Type == MessageActionType.Delete)
                .Where((m, a) => a.Message_Id == null)   // 过滤已删除
                .Where((m, a) => m.Target_Id == groupId && m.Chat_Type == ChatType.Group)
                .WhereIF(skipMessageId > 0, (m, a) => m.Id < skipMessageId)
                .OrderBy((m, a) => m.Id, OrderByType.Desc)
                .Take(take)
                .ToListAsync();
        }



        public async Task<bool> RecallMessage(long messageId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var result = await db.Updateable<MessageEntity>()
                .SetColumns(m => m.Message_Status == MessageStatus.Recalled)
                .SetColumns(m => m.Content == null)
                .Where(m => m.Id == messageId)
                .ExecuteCommandAsync();
            return result > 0;
        }
    }
}
