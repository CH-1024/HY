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

        Task RequestContact(long userId, string hyid);
        Task ResponseContact(long userId, string hyid);
    }


    public class ContactService : IContactService
    {
        private readonly IUserRepository _userRepository;
        private readonly IContactRepository _contactRepository;
        private readonly IContactRequestRepository _contactRequestRepository;


        public ContactService(IUserRepository userRepository, IContactRepository contactRepository, IContactRequestRepository contactRequestRepository)
        {
            _userRepository = userRepository;
            _contactRepository = contactRepository;
            _contactRequestRepository = contactRequestRepository;
        }



        public async Task<List<ContactDto>> GetContactsByUserId(long userId)
        {
            var contactEntities = await _contactRepository.GetContactsByUserId(userId);

            var contactDtos = contactEntities.Adapt<List<ContactDto>>();

            var contactIds = contactEntities.Select(c => c.Contact_Id).Distinct().ToList();
            var contactRequestIds = contactEntities.Select(c => c.Contact_Request_Id).ToList();

            var contactMap = (await _userRepository.GetUsersByIds(contactIds)).ToDictionary(x => x.Id);
            var contactRequestMap = (await _contactRequestRepository.GetContactRequestsByIds(contactRequestIds)).ToDictionary(x => x.Id);

            foreach (var contactDto in contactDtos)
            {
                if (contactMap.TryGetValue(contactDto.Contact_Id.Value, out var contact))
                {
                    contactDto.HYid = contact.HYid;
                    contactDto.Nickname = contact.Nickname;
                    contactDto.Avatar = contact.Avatar;
                    contactDto.Region = contact.Region;
                }

                if (contactRequestMap.TryGetValue(contactDto.Contact_Request_Id.Value, out var contactRequest))
                {
                    contactDto.Relation_Request_Status = contactRequest.Relation_Request_Status;
                }
            }

            return contactDtos;
        }

        public async Task<ContactDto> GetContactByUserId(long currentUserId, long targetId)
        {
            var target = await _userRepository.GetUserById(targetId);
            if (target == null) throw new Exception("用户不存在");

            var contactEntity = await _contactRepository.GetUserContactByUserId(currentUserId, targetId);
            var contactRequestEntity = await _contactRequestRepository.GetContactRequestById(contactEntity.Contact_Request_Id);

            if (contactEntity != null)
            {
                var contactDto = contactEntity.Adapt<ContactDto>();
                contactDto.HYid = target.HYid;
                contactDto.Nickname = target.Nickname;
                contactDto.Avatar = target.Avatar;
                contactDto.Region = target.Region;
                contactDto.Contact_Status = target.Status;

                contactDto.Relation_Request_Status = contactRequestEntity.Relation_Request_Status;

                return contactDto;
            }
            else
            {
                return CreatNoneRelationContact(target.HYid, target.Nickname, target.Avatar, target.Region);
            }
        }

        public async Task<List<ContactDto>> GetContactByHYidOrPhone(long currentUserId, string identity)
        {
            var userEntities = await _userRepository.GetUserByHYidOrPhone(identity);

            var userIds = userEntities.Select(u => u.Id).Distinct().ToList();

            var contactMap = (await _contactRepository.GetUserContactByUserIds(currentUserId, userIds)).ToDictionary(c => c.Contact_Id);

            var contactRequestIds = contactMap.Values.Select(c => c.Contact_Request_Id).Distinct().ToList();

            var contactRequestMap = (await _contactRequestRepository.GetContactRequestsByIds(contactRequestIds)).ToDictionary(x => x.Id);

            var contactDtos = new List<ContactDto>();

            foreach (var userEntity in userEntities)
            {
                if (contactMap.TryGetValue(userEntity.Id, out var contactEntity) && contactRequestMap.TryGetValue(contactEntity.Contact_Request_Id, out var contactRequest))
                {
                    var contactDto = contactEntity.Adapt<ContactDto>();
                    contactDto.HYid = userEntity.HYid;
                    contactDto.Nickname = userEntity.Nickname;
                    contactDto.Avatar = userEntity.Avatar;
                    contactDto.Region = userEntity.Region;
                    contactDto.Contact_Status = userEntity.Status;

                    contactDto.Relation_Request_Status = contactRequest.Relation_Request_Status;

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


        public async Task RequestContact(long senderId, string targetHYid)
        {
            var target = await _userRepository.GetUserByHYid(targetHYid);
            if (target == null) throw new Exception("用户不存在");

            var senderContact = await _contactRepository.GetUserContactByUserId(senderId, target.Id);
            if (senderContact != null)
            {
                // 更新联系人记录
            }
            else
            {
                // 创建联系人记录
            }


            //var contactEntity = new ContactEntity
            //{
            //    User_Id = senderId,
            //    Contact_Id = target.Id,
            //    Created_At = DateTime.UtcNow
            //};
            //await _contactRepository.AddContact(contactEntity);
        }

        public async Task ResponseContact(long senderId, string targetHYid)
        {
        
        }






        ContactDto CreatNoneRelationContact(string? hyid, string? nickname, string? avatar, string? region)
        {
            return new ContactDto
            {
                Contact_Id = null,
                HYid = hyid,
                Nickname = nickname,
                Avatar = avatar,
                Region = region,
                Remark = null,
                Contact_Status = null,
                Relation_Request_Status = RelationRequestStatus.None,
                Relation_Status = RelationStatus.None,
                Created_At = null
            };
        }

    }
}
