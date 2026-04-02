using HY.ApiService.Dtos;
using HY.ApiService.Entities;
using HY.ApiService.Enums;
using HY.ApiService.Models;
using HY.ApiService.Repositories;
using Mapster;

namespace HY.ApiService.Services
{
    public interface IContactService
    {
        Task<ContactResult> GetAllContactRequestsByUserId(long userId);

        Task<ContactResult> GetAllContactsByUserId(long userId);
        Task<ContactResult> GetContactByUserId(long currentUserId, long targetId);
        Task<ContactResult> GetContactByHYidOrPhone(long currentUserId, string identity);

        Task<ContactRequestDto?> RequestContact(long userId, long contactId, string source, string message);
        Task RespondContact(long userId, string hyid, string messag);
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


        public async Task<ContactResult> GetAllContactRequestsByUserId(long currentUserId)
        {
            var contactRequestEntities = await _contactRequestRepository.GetContactRequestsByUserId(currentUserId);

            var contactRequestDtos = contactRequestEntities.Adapt<List<ContactRequestDto>>();

            var senderIds = contactRequestEntities.FindAll(cr => cr.Receiver_Id == currentUserId).Select(cr => cr.Sender_Id).Distinct().ToList();
            var receiverIds = contactRequestEntities.FindAll(cr => cr.Sender_Id == currentUserId).Select(cr => cr.Receiver_Id).Distinct().ToList();

            var userIds = senderIds.Union(receiverIds).ToList();

            var userMap = (await _userRepository.GetUsersByIds(userIds)).ToDictionary(u => u.Id);

            foreach (var contactRequestDto in contactRequestDtos)
            {
                if (userMap.TryGetValue(contactRequestDto.Sender_Id, out var sender))
                {
                    contactRequestDto.Sender_Nickname = sender.Nickname;
                    contactRequestDto.Sender_Avatar = sender.Avatar;
                }
                if (userMap.TryGetValue(contactRequestDto.Receiver_Id, out var receiver))
                {
                    contactRequestDto.Receiver_Nickname = receiver.Nickname;
                    contactRequestDto.Receiver_Avatar = receiver.Avatar;
                }
            }

            return new ContactResult(true, null, null, null, null, contactRequestDtos);
        }

        public async Task<ContactResult> GetAllContactsByUserId(long currentUserId)
        {
            var contactEntities = await _contactRepository.GetContactsByUserId(currentUserId);

            var contactDtos = contactEntities.Adapt<List<ContactDto>>();

            var contactIds = contactEntities.Select(c => c.Contact_Id).Distinct().ToList();
            var contactRequestIds = contactEntities.Select(c => c.Contact_Request_Id).ToList();

            var contactMap = (await _userRepository.GetUsersByIds(contactIds)).ToDictionary(x => x.Id);
            var contactRequestMap = (await _contactRequestRepository.GetContactRequestsByIds(contactRequestIds)).ToDictionary(x => x.Id);

            foreach (var contactDto in contactDtos)
            {
                if (contactMap.TryGetValue(contactDto.Contact_Id.Value, out var contact))
                {
                    contactDto.Nickname = contact.Nickname;
                    contactDto.Avatar = contact.Avatar;
                    contactDto.Region = contact.Region;
                }

                if (contactRequestMap.TryGetValue(contactDto.Contact_Request_Id.Value, out var contactRequest))
                {
                    contactDto.Relation_Request_Status = contactRequest.Relation_Request_Status;
                }
            }

            return new ContactResult(true, null, null, contactDtos);
        }

        public async Task<ContactResult> GetContactByUserId(long currentUserId, long targetId)
        {
            var target = await _userRepository.GetUserById(targetId);
            if (target == null) return new ContactResult(false, "用户不存在");

            ContactDto? contactDto = null;

            var contactEntity = await _contactRepository.GetUserContactByUserId(currentUserId, targetId);
            if (contactEntity == null)
            {
                contactDto = CreatNoneRelationContact(target.Id, target.Nickname, target.Avatar, target.Region);
                return new ContactResult(true, null, contactDto);
            }
            else
            {
                contactDto = contactEntity.Adapt<ContactDto>();
                contactDto.Nickname = target.Nickname;
                contactDto.Avatar = target.Avatar;
                contactDto.Region = target.Region;
                contactDto.Contact_Status = target.Status;

                var contactRequestEntity = await _contactRequestRepository.GetContactRequestById(contactEntity.Contact_Request_Id);
                contactDto.Relation_Request_Status = contactRequestEntity.Relation_Request_Status;

                return new ContactResult(true, null, contactDto);
            }
        }

        public async Task<ContactResult> GetContactByHYidOrPhone(long currentUserId, string identity)
        {
            var userEntities = await _userRepository.GetUserByHYidOrPhone(identity);

            var userIds = userEntities.Select(u => u.Id).Distinct().ToList();

            var contactMap = (await _contactRepository.GetUserContactByUserIds(currentUserId, userIds)).ToDictionary(c => c.Contact_Id);

            var contactRequestIds = contactMap.Values.Select(c => c.Contact_Request_Id).Distinct().ToList();

            var contactRequestMap = (await _contactRequestRepository.GetContactRequestsByIds(contactRequestIds)).ToDictionary(x => x.Id);

            var contactDtos = new List<ContactDto>();

            foreach (var userEntity in userEntities)
            {
                if (contactMap.TryGetValue(userEntity.Id, out var contactEntity))
                {
                    var contactDto = contactEntity.Adapt<ContactDto>();
                    contactDto.Nickname = userEntity.Nickname;
                    contactDto.Avatar = userEntity.Avatar;
                    contactDto.Region = userEntity.Region;
                    contactDto.Contact_Status = userEntity.Status;

                    if (contactRequestMap.TryGetValue(contactEntity.Contact_Request_Id, out var contactRequest))
                    {
                        contactDto.Relation_Request_Status = contactRequest.Relation_Request_Status;
                    }

                    contactDtos.Add(contactDto);
                }
                else
                {
                    var contactDto = CreatNoneRelationContact(userEntity.Id, userEntity.Nickname, userEntity.Avatar, userEntity.Region);
                    contactDtos.Add(contactDto);
                }
            }

            return new ContactResult(true, null, null, contactDtos);
        }


        public async Task<ContactRequestDto?> RequestContact(long senderId, long contactId, string source, string message)
        {
            var sender = await _userRepository.GetUserById(senderId);
            if (sender == null) return null;

            var target = await _userRepository.GetUserById(contactId);
            if (target == null) return null;

            var senderContact = await _contactRepository.GetUserContactByUserId(senderId, target.Id);
            if (senderContact != null && senderContact.Relation_Status == RelationStatus.Friend)
            {
                return null;
            }

            if (await _contactRequestRepository.ExistsPendingContactRequest(senderId, target.Id))
            {
                return null;
            }
            else
            {
                // 创建联系人请求
                var contactRequestEntity = new ContactRequestEntity
                {
                    Sender_Id = senderId,
                    Receiver_Id = target.Id,
                    Message = message,
                    Source = source,
                    Relation_Request_Status = RelationRequestStatus.Pending,
                    Created_At = DateTime.UtcNow
                };
                await _contactRequestRepository.CreateContactRequest(contactRequestEntity);

                var contactRequestDto = contactRequestEntity.Adapt<ContactRequestDto>();

                contactRequestDto.Sender_Avatar = sender.Avatar;
                contactRequestDto.Sender_Nickname = sender.Nickname;
                contactRequestDto.Receiver_Avatar = target.Avatar;
                contactRequestDto.Receiver_Nickname = target.Nickname;

                return contactRequestDto;
            }
        }

        public async Task RespondContact(long senderId, string targetHYid, string messag)
        {
            var target = await _userRepository.GetUserByHYid(targetHYid);
            //if (target == null) return new ContactResult(false, "用户不存在");

        }






        ContactDto CreatNoneRelationContact(long? userId, string? nickname, string? avatar, string? region)
        {
            return new ContactDto
            {
                Contact_Id = userId,
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
