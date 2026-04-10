using HY.ApiService.Dtos;
using HY.ApiService.Repositories;
using Mapster;

namespace HY.ApiService.Services
{
    public interface IGroupMemberService
    {
        Task<GroupMemberDto> GetGroupMember(long group_Id, long target_Id);
        Task<List<GroupMemberDto>> GetGroupMembersByGroupId(long groupId);
    }


    public class GroupMemberService : IGroupMemberService
    {
        private readonly IGroupRepository _groupRepository;
        private readonly IGroupMemberRepository _groupMemberRepository;



        public GroupMemberService(IGroupRepository groupRepository, IGroupMemberRepository groupMemberRepository)
        {
            _groupRepository = groupRepository;
            _groupMemberRepository = groupMemberRepository;
        }


        public async Task<GroupMemberDto> GetGroupMember(long group_Id, long target_Id)
        {
            var groupMemberEntity = await _groupMemberRepository.GetGroupMember(group_Id, target_Id);
            return groupMemberEntity.Adapt<GroupMemberDto>();
        }

        public async Task<List<GroupMemberDto>> GetGroupMembersByGroupId(long groupId)
        {
            var groupMemberEntities = await _groupMemberRepository.GetGroupMembersByGroupId(groupId);
            return groupMemberEntities.Adapt<List<GroupMemberDto>>();
        }

    }

}
