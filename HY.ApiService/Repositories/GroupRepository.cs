
using HY.ApiService.Entities;
using SqlSugar;

namespace HY.ApiService.Repositories
{
    public interface IGroupRepository
    {
        Task<GroupEntity> GetGroupById(long groupId);
        Task<List<GroupEntity>> GetGroupsByIds(List<long> groupIds);
    }


    public class GroupRepository : IGroupRepository
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public GroupRepository(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }


        public async Task<GroupEntity> GetGroupById(long groupId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            return await db.Queryable<GroupEntity>()
                .Where(g => g.Id == groupId)
                .SingleAsync();
        }

        public async Task<List<GroupEntity>> GetGroupsByIds(List<long> groupIds)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            return await db.Queryable<GroupEntity>().In(groupIds).ToListAsync();
        }

    }
}
