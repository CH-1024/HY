
using HY.ApiService.Entities;
using SqlSugar;

namespace HY.ApiService.Repositories
{
    public interface IGroupMemberRepository
    {
        Task<List<GroupMemberEntity>> GetGroupMembersByGroupId(long target_Id);
    }


    public class GroupMemberRepository : IGroupMemberRepository
    {
        private readonly ISqlSugarClient _db;

        public GroupMemberRepository(ISqlSugarClient db)
        {
            _db = db;
        }


        public async Task<List<GroupMemberEntity>> GetGroupMembersByGroupId(long groupId)
        {
            return await _db.Queryable<GroupMemberEntity>()
                .Where(gm => gm.Group_Id == groupId)
                .ToListAsync();
        }

    }

}
