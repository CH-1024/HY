using HY.ApiService.Entities;
using HY.ApiService.Enums;
using SqlSugar;

namespace HY.ApiService.Repositories
{
    public interface IContactRequestRepository
    {
        Task<long> CreateContactRequest(ContactRequestEntity contactRequestEntity);

        Task<List<ContactRequestEntity>> GetContactRequestsByUserId(long userId);
        Task<ContactRequestEntity?> GetContactRequestById(long contact_Request_Id);
        Task<List<ContactRequestEntity>> GetContactRequestsByIds(List<long> contactRequestIds);
        Task<ContactRequestEntity?> GetPendingContactRequestByUserId(long senderId, long targetId);

        Task<bool> UpdateContactRequest(ContactRequestEntity contactRequestEntity);
    }


    public class ContactRequestRepository : IContactRequestRepository
    {
        private readonly ISqlSugarClient _db;

        public ContactRequestRepository(ISqlSugarClient db)
        {
            _db = db;
        }



        public async Task<long> CreateContactRequest(ContactRequestEntity contactRequest)
        {
            contactRequest.Id = await _db.Insertable(contactRequest).ExecuteReturnBigIdentityAsync();
            return contactRequest.Id;
        }



        //public async Task<List<ContactRequestEntity>> GetContactRequestsByUserId(long userId)
        //{
        //    return await _db.Queryable<ContactRequestEntity>()
        //        .Where(c => c.Sender_Id == userId || c.Receiver_Id == userId)
        //        .ToListAsync();
        //}

        public async Task<List<ContactRequestEntity>> GetContactRequestsByUserId(long userId)
        {
            var query = _db.Queryable<ContactRequestEntity>()
                .Where(c => c.Sender_Id == userId || c.Receiver_Id == userId);

            // 先取每组最新ID
            var latestIds = await query
                .GroupBy(c => new
                {
                    MinId = SqlFunc.IIF(c.Sender_Id < c.Receiver_Id, c.Sender_Id, c.Receiver_Id),
                    MaxId = SqlFunc.IIF(c.Sender_Id > c.Receiver_Id, c.Sender_Id, c.Receiver_Id)
                })
                .Select(c => SqlFunc.AggregateMax(c.Id))
                .ToListAsync();

            // 再查询实体
            return await _db.Queryable<ContactRequestEntity>()
                .In(latestIds).ToListAsync();
        }

        public async Task<ContactRequestEntity?> GetContactRequestById(long contact_Request_Id)
        {
            return await _db.Queryable<ContactRequestEntity>()
                .Where(c => c.Id == contact_Request_Id)
                .SingleAsync();
        }

        public async Task<List<ContactRequestEntity>> GetContactRequestsByIds(List<long> contactRequestIds)
        {
            return await _db.Queryable<ContactRequestEntity>().In(contactRequestIds).ToListAsync();
        }

        public async Task<ContactRequestEntity?> GetPendingContactRequestByUserId(long senderId, long targetId)
        {
            return await _db.Queryable<ContactRequestEntity>()
                .Where(c => c.Sender_Id == senderId && c.Receiver_Id == targetId && c.Relation_Request_Status == RelationRequestStatus.Pending)
                .SingleAsync();
        }



        public async Task<bool> UpdateContactRequest(ContactRequestEntity contactRequestEntity)
        {
            return await _db.Updateable(contactRequestEntity).ExecuteCommandAsync() > 0;
        }

    }
}
