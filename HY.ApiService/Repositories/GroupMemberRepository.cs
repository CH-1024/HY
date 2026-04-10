
using HY.ApiService.Entities;
using SqlSugar;

namespace HY.ApiService.Repositories
{
    public interface IGroupMemberRepository
    {
        Task<GroupMemberEntity> GetGroupMember(long group_Id, long target_Id);
        Task<List<GroupMemberEntity>> GetGroupMembersByGroupId(long groupId);
    }


    public class GroupMemberRepository : IGroupMemberRepository
    {
        private readonly ISqlSugarClient _db;

        public GroupMemberRepository(ISqlSugarClient db)
        {
            _db = db;
        }



        public async Task<GroupMemberEntity> GetGroupMember(long group_Id, long target_Id)
        {
            return await _db.Queryable<GroupMemberEntity>()
                .Where(gm => gm.Group_Id == group_Id && gm.User_Id == target_Id)
                .SingleAsync();
        }

        public async Task<List<GroupMemberEntity>> GetGroupMembersByGroupId(long groupId)
        {
            return await _db.Queryable<GroupMemberEntity>()
                .Where(gm => gm.Group_Id == groupId)
                .ToListAsync();
        }

    }

}
