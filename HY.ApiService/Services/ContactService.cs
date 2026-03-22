using HY.ApiService.Dtos;
using HY.ApiService.Repositories;
using Mapster;

namespace HY.ApiService.Services
{
    public interface IContactService
    {
        Task<List<ContactDto>> GetContactsByUserId(long userId);
        Task<StrangerDto> GetStrangerByUserId(long strangerId);
        Task<List<ContactDto>> GetContactByHYidOrUsername(long currentUserId, string identity);
    }


    public class ContactService : IContactService
    {
        private readonly IUserRepository _userRepository;
        private readonly IContactRepository _contactRepository;


        public ContactService(IUserRepository userRepository, IContactRepository contactRepository)
        {
            _userRepository = userRepository;
            _contactRepository = contactRepository;
        }



        public async Task<List<ContactDto>> GetContactsByUserId(long userId)
        {
            var contactEntities = await _contactRepository.GetContactsByUserId(userId);

            var contactDtos = contactEntities.Adapt<List<ContactDto>>();

            var contactIds = contactDtos.Select(c => c.Contact_Id).Distinct().ToList();

            var userMap = (await _userRepository.GetUsersByIds(contactIds)).ToDictionary(x => x.Id);

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
            var userEntity = await _userRepository.GetUserById(strangerId);
            var strangerDto = userEntity.Adapt<StrangerDto>();
            return strangerDto;
        }

        public async Task<List<ContactDto>> GetContactByHYidOrUsername(long currentUserId, string identity)
        {
            var userEntities = await _userRepository.GetUserByHYidOrUsername(identity);

            var userIds = userEntities.Select(u => u.Id).Distinct().ToList();

            var contactMap = (await _contactRepository.GetUserContactByUserIds(currentUserId, userIds)).ToDictionary(c => c.Contact_Id);

            var contactDtos = new List<ContactDto>();

            foreach (var user in userEntities)
            {
                if (contactMap.TryGetValue(user.Id, out var contactEntity))
                {
                    var contactDto = contactEntity.Adapt<ContactDto>();
                    contactDto.HYid = user.HYid;
                    contactDto.Nickname = user.Nickname;
                    contactDto.Avatar = user.Avatar;
                    contactDto.Region = user.Region;
                    contactDtos.Add(contactDto);
                }
                else
                {
                    var contactDto = new ContactDto
                    {
                        Contact_Id = 0,
                        HYid = user.HYid,
                        Nickname = user.Nickname,
                        Avatar = user.Avatar,
                        Region = user.Region,
                        Contact_Status = UserStatus.Active,
                        Relation_Status = RelationStatus.None,
                        Created_At = DateTime.UtcNow
                    };
                    contactDtos.Add(contactDto);
                }
            }

            return contactDtos;
        }

    }
}
