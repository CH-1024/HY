using HY.ApiService.Dtos;
using HY.ApiService.Enums;
using HY.ApiService.Repositories;
using Mapster;

namespace HY.ApiService.Services
{
    public interface IContactService
    {
        Task<List<ContactDto>> GetContactsByUserId(long userId);
        Task<ContactDto> GetContactByUserId(long currentUserId, long targetId);
        Task<List<ContactDto>> GetContactByHYidOrPhone(long currentUserId, string identity);

        Task AddContact(long userId, string hyid);
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

        public async Task<ContactDto> GetContactByUserId(long currentUserId, long targetId)
        {
            var userEntity = await _userRepository.GetUserById(targetId);

            var contactEntity = await _contactRepository.GetUserContactByUserId(currentUserId, targetId);

            if (contactEntity != null)
            {
                var contactDto = contactEntity.Adapt<ContactDto>();
                contactDto.HYid = userEntity.HYid;
                contactDto.Nickname = userEntity.Nickname;
                contactDto.Avatar = userEntity.Avatar;
                contactDto.Region = userEntity.Region;
                contactDto.Contact_Status = userEntity.Status;
                return contactDto;
            }
            else
            {
                return CreatNoneRelationContact(userEntity.HYid, userEntity.Nickname, userEntity.Avatar, userEntity.Region);
            }
        }

        public async Task<List<ContactDto>> GetContactByHYidOrPhone(long currentUserId, string identity)
        {
            var userEntities = await _userRepository.GetUserByHYidOrPhone(identity);

            var userIds = userEntities.Select(u => u.Id).Distinct().ToList();

            var contactMap = (await _contactRepository.GetUserContactByUserIds(currentUserId, userIds)).ToDictionary(c => c.Contact_Id);

            var contactDtos = new List<ContactDto>();

            foreach (var userEntity in userEntities)
            {
                if (contactMap.TryGetValue(userEntity.Id, out var contactEntity))
                {
                    var contactDto = contactEntity.Adapt<ContactDto>();
                    contactDto.HYid = userEntity.HYid;
                    contactDto.Nickname = userEntity.Nickname;
                    contactDto.Avatar = userEntity.Avatar;
                    contactDto.Region = userEntity.Region;
                    contactDto.Contact_Status = userEntity.Status;
                    contactDtos.Add(contactDto);
                }
                else
                {
                    var contactDto = CreatNoneRelationContact(userEntity.HYid, userEntity.Nickname, userEntity.Avatar, userEntity.Region);
                    contactDtos.Add(contactDto);
                }
            }

            return contactDtos;
        }


        public async Task AddContact(long senderId, string targetHYid)
        {
            var target = await _userRepository.GetUserByHYid(targetHYid);
            if (target == null) throw new Exception("用户不存在");

            var senderContact = await _contactRepository.GetUserContactByUserId(senderId, target.Id);
            if (senderContact != null)
            {
                // 更新联系人记录

                senderContact.Relation_Status = RelationStatus.Pending;
            }
            else
            {
                // 创建联系人记录
            }


            var contactEntity = new ContactEntity
            {
                User_Id = senderId,
                Contact_Id = target.Id,
                Created_At = DateTime.UtcNow
            };
            await _contactRepository.AddContact(contactEntity);
        }


        ContactDto CreatNoneRelationContact(string? hyid, string? nickname, string? avatar, string? region)
        {
            return new ContactDto
            {
                Contact_Id = 0,
                HYid = hyid,
                Nickname = nickname,
                Avatar = avatar,
                Region = region,
                Contact_Status = UserStatus.Unknown,
                Relation_Status = RelationStatus.None,
                Created_At = DateTime.UtcNow
            };
        }

    }
}
