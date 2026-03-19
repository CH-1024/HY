using HY.ApiService.Entities;
using HY.ApiService.Enums;
using SqlSugar;

namespace HY.ApiService.Repositories
{
    public interface IMessageActionRepository
    {
        Task<bool> InsertMessageAction(MessageActionEntity messageActionEntity);

        Task<MessageActionEntity> GetMessageActionByUserIdAndMessageId(long userId, long messageId, MessageActionType actionType);
        Task<List<MessageActionEntity>> GetMessageActionsByUserIdAndMessageId(long userId, long messageId);
    }


    public class MessageActionRepository : IMessageActionRepository
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public MessageActionRepository(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }




        public async Task<bool> InsertMessageAction(MessageActionEntity messageActionEntity)
        {
            // 处理重复插入带复合主键的数据

            // 方式一
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var count = await db.Insertable(messageActionEntity).IgnoreInsertError().ExecuteCommandAsync();
            return count > 0;

            // 方式二
            //using var scope = _scopeFactory.CreateScope();
            //var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            //var sql = "INSERT IGNORE INTO message_user_action (User_Id, Message_Id, Action_Type, Created_At) VALUES (@u,@m,@t,@c)";
            //var affected = await db.Ado.ExecuteCommandAsync(sql, new { u = messageActionEntity.User_Id, m = messageActionEntity.Message_Id, t = (int)messageActionEntity.Action_Type, c = messageActionEntity.Created_At });
            //return affected > 0 || affected == 0; // IGNORE 时已存在返回 0，仍可视为成功

            // 方式三
            //using var scope = _scopeFactory.CreateScope();
            //var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            //// 使用 Storageable 处理单个实体的插入忽略逻辑
            //var storage = db.Storageable(messageActionEntity)
            //    // 指定用于判断冲突的唯一键列（需与数据库中的唯一约束一致）
            //    .WhereColumns(
            //    [
            //        nameof(messageActionEntity.User_Id),
            //        nameof(messageActionEntity.Message_Id),
            //        nameof(messageActionEntity.Action_Type)
            //    ])
            //    // 如果数据不存在则插入
            //    .SplitInsert(it => it.NotAny())
            //    // 如果数据已存在则忽略（不进行任何操作）
            //    .SplitIgnore(it => it.Any())
            //    // 返回实际插入的行数（忽略时为0）
            //    .ExecuteCommand();

            //return storage >= 0; // 等同于 true，也可直接 return true;
        }


        public async Task<MessageActionEntity> GetMessageActionByUserIdAndMessageId(long userId, long messageId, MessageActionType actionType)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            return await db.Queryable<MessageActionEntity>()
                .Where(a => a.Message_Id == messageId && a.User_Id == userId && a.Action_Type == actionType)
                .SingleAsync();
        }

        public async Task<List<MessageActionEntity>> GetMessageActionsByUserIdAndMessageId(long userId, long messageId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            return await db.Queryable<MessageActionEntity>()
                .Where(a => a.Message_Id == messageId && a.User_Id == userId)
                .ToListAsync();
        }

    }
}
