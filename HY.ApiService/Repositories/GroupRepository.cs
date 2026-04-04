
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
        private readonly ISqlSugarClient _db;

        public GroupRepository(ISqlSugarClient db)
        {
            _db = db;
        }


        public async Task<GroupEntity> GetGroupById(long groupId)
        {
            return await _db.Queryable<GroupEntity>()
                .Where(g => g.Id == groupId)
                .SingleAsync();
        }

        public async Task<List<GroupEntity>> GetGroupsByIds(List<long> groupIds)
        {
            return await _db.Queryable<GroupEntity>().In(groupIds).ToListAsync();
        }

    }
}
