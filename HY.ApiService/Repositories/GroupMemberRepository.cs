
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
        private readonly IServiceScopeFactory _scopeFactory;

        public GroupMemberRepository(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }


        public async Task<List<GroupMemberEntity>> GetGroupMembersByGroupId(long groupId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            return await db.Queryable<GroupMemberEntity>()
                .Where(gm => gm.Group_Id == groupId)
                .ToListAsync();
        }

    }

}
