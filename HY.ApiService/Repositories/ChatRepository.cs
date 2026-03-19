
using HY.ApiService.Dtos;
using HY.ApiService.Entities;
using HY.ApiService.Enums;
using SqlSugar;

namespace HY.ApiService.Repositories
{
    public interface IChatRepository
    {
        Task<ChatEntity> GetChatByChatId(long chatId);
        Task<ChatEntity> GetChatByUserIdAndType(long userId, long targetId, ChatType chatType);
        Task<List<ChatEntity>> GetChatsByUserIdsAndType(List<long> userIds, long target_Id, ChatType chatType);
        Task<List<ChatEntity>> GetChatsByUserId(long userId);

        Task<bool> UpdateChat(ChatEntity senderChatEntity);
        Task<bool> UpdateChatUnread(long chatId);
        Task<bool> UpdateChats(List<ChatEntity> memberChatEntities);
    }


    public class ChatRepository : IChatRepository
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ChatRepository(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }



        public async Task<ChatEntity> GetChatByChatId(long chatId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            return await db.Queryable<ChatEntity>()
                .Where(c => c.Id == chatId)
                .SingleAsync();
        }

        public async Task<ChatEntity> GetChatByUserIdAndType(long userId, long targetId, ChatType chatType)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            return await db.Queryable<ChatEntity>()
                .Where(c => c.User_Id == userId && c.Target_Id == targetId && c.Type == chatType)
                .SingleAsync();
        }

        public async Task<List<ChatEntity>> GetChatsByUserIdsAndType(List<long> userIds, long target_Id, ChatType chatType)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            return await db.Queryable<ChatEntity>()
                .Where(c => userIds.Contains(c.User_Id) && c.Target_Id == target_Id && c.Type == chatType)
                .ToListAsync();
        }

        public async Task<List<ChatEntity>> GetChatsByUserId(long userId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            return await db.Queryable<ChatEntity>()
                .Where(c => c.User_Id == userId)
                .OrderBy(c => c.Last_Msg_Time, OrderByType.Desc)
                .ToListAsync();
        }



        public async Task<bool> UpdateChat(ChatEntity senderChatEntity)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var result = await db.Updateable(senderChatEntity).ExecuteCommandAsync();
            return result > 0;
        }

        public async Task<bool> UpdateChatUnread(long chatId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var result = await db.Updateable<ChatEntity>()
                .SetColumns(c => c.Unread_Count == 0)
                .Where(c => c.Id == chatId)
                .ExecuteCommandAsync();
            return result > 0;
        }

        public async Task<bool> UpdateChats(List<ChatEntity> memberChatEntities)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var result = await db.Updateable(memberChatEntities).ExecuteCommandAsync();
            return result > 0;
        }

        //public async Task<List<ChatDto>> GetChatsByUserId(long userId)
        //{
        //    using var scope = _scopeFactory.CreateScope();
        //    var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        //    var userChats = await db.Queryable<ChatEntity, UserEntity, MessageEntity>(
        //        (c, u, m) => new JoinQueryInfos(
        //            JoinType.Left, c.Target_Id == u.Id,
        //            JoinType.Left, c.Last_Msg_Id == m.Id
        //        ))
        //        .Where(c => c.User_Id == userId && c.Type == 1)
        //        .Select((c, u, m) => new ChatDto
        //        {
        //            Id = c.Id,
        //            Type = c.Type,
        //            Target_Id = c.Target_Id,
        //            Target_Name = u.Nickname,
        //            Target_Avatar = u.Avatar,
        //            Last_Msg_Id = c.Last_Msg_Id,
        //            Read_Msg_Id = c.Read_Msg_Id,
        //            Unread_Count = c.Unread_Count,
        //            Is_Top = c.Is_Top,
        //            Is_Deleted = c.Is_Deleted,
        //            Last_Msg_Time = c.Last_Msg_Time,
        //            Last_Message_Brief = m.Content == null ? string.Empty : m.Content.Substring(0, Math.Min(m.Content.Length, 15))
        //        }).ToListAsync();

        //    var groupChats = await db.Queryable<ChatEntity, GroupEntity, MessageEntity>(
        //        (c, g, m) => new JoinQueryInfos(
        //            JoinType.Left, c.Target_Id == g.Id,
        //            JoinType.Left, c.Last_Msg_Id == m.Id
        //        ))
        //        .Where(c => c.User_Id == userId && c.Type == 2)
        //        .Select((c, g, m) => new ChatDto
        //        {
        //            Id = c.Id,
        //            Type = c.Type,
        //            Target_Id = c.Target_Id,
        //            Target_Name = g.Name,
        //            Target_Avatar = g.Avatar,
        //            Last_Msg_Id = c.Last_Msg_Id,
        //            Read_Msg_Id = c.Read_Msg_Id,
        //            Unread_Count = c.Unread_Count,
        //            Is_Top = c.Is_Top,
        //            Is_Deleted = c.Is_Deleted,
        //            Last_Msg_Time = c.Last_Msg_Time,
        //            Last_Message_Brief = m.Content == null ? string.Empty : m.Content.Substring(0, Math.Min(m.Content.Length, 15))
        //        }).ToListAsync();

        //    var chats = userChats.Concat(groupChats).OrderByDescending(c => c.Last_Msg_Time).ToList();

        //    return chats;
        //}

    }
}
