using HY.ApiService.Dtos;
using HY.ApiService.Repositories;
using Mapster;

namespace HY.ApiService.Services
{
    public interface IContactService
    {
        Task<List<ContactDto>> GetContactsByUserId(long userId);
        Task<StrangerDto> GetStrangerByUserId(long strangerId);
    }


    public class ContactService : IContactService
    {
        private readonly IUserRepository _userRepositoryp;
        private readonly IContactRepository _contactRepository;


        public ContactService(IUserRepository userRepository, IContactRepository contactRepository)
        {
            _userRepositoryp = userRepository;
            _contactRepository = contactRepository;
        }



        public async Task<List<ContactDto>> GetContactsByUserId(long userId)
        {
            var contactEntities = await _contactRepository.GetContactsByUserId(userId);

            var contactDtos = contactEntities.Adapt<List<ContactDto>>();

            var contactIds = contactDtos.Select(c => c.Contact_Id).Distinct().ToList();

            var userMap = (await _userRepositoryp.GetUsersByIds(contactIds)).ToDictionary(x => x.Id);

            foreach (var contactDto in contactDtos)
            {
                if (userMap.TryGetValue(contactDto.Contact_Id, out var user)) // 单聊
                {
                    contactDto.HYid = user.HYid;
                    contactDto.Nickname = user.Nickname;
                    contactDto.Avatar = user.Avatar;
                    contactDto.Region = user.Region;
                }
            }

            return contactDtos;
        }

        public async Task<StrangerDto> GetStrangerByUserId(long strangerId)
        {
            var userEntity = await _userRepositoryp.GetUserById(strangerId);
            var strangerDto = userEntity.Adapt<StrangerDto>();
            return strangerDto;
        }

    }
}
