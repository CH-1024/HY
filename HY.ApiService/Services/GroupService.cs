using HY.ApiService.Repositories;

namespace HY.ApiService.Services
{
    public interface IGroupService
    {
    
    }


    public class GroupService : IGroupService
    {
        private readonly IGroupRepository _groupRepository;


        public GroupService(IGroupRepository groupRepository)
        {
            _groupRepository = groupRepository;
        }


    }
}
