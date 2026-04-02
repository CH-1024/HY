using HY.ApiService.Entities;
using HY.ApiService.Enums;
using SqlSugar;

namespace HY.ApiService.Repositories
{
    public interface IContactRequestRepository
    {
        Task<long> CreateContactRequest(ContactRequestEntity contactRequestEntity);

        Task<List<ContactRequestEntity>> GetContactRequestsByUserId(long userId);
        Task<ContactRequestEntity> GetContactRequestById(long contact_Request_Id);
        Task<List<ContactRequestEntity>> GetContactRequestsByIds(List<long> contactRequestIds);

        Task<bool> ExistsPendingContactRequest(long senderId, long targetId);
    }


    public class ContactRequestRepository : IContactRequestRepository
    {
        private readonly IServiceScopeFactory _scopeFactory;


        public ContactRequestRepository(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }



        public async Task<long> CreateContactRequest(ContactRequestEntity contactRequest)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            contactRequest.Id = await db.Insertable(contactRequest).ExecuteReturnBigIdentityAsync();
            return contactRequest.Id;
        }



        public async Task<List<ContactRequestEntity>> GetContactRequestsByUserId(long userId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            return await db.Queryable<ContactRequestEntity>()
                .Where(c => c.Sender_Id == userId || c.Receiver_Id == userId)
                .ToListAsync();
        }

        public async Task<ContactRequestEntity> GetContactRequestById(long contact_Request_Id)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            return await db.Queryable<ContactRequestEntity>()
                .Where(c => c.Id == contact_Request_Id)
                .SingleAsync();
        }

        public async Task<List<ContactRequestEntity>> GetContactRequestsByIds(List<long> contactRequestIds)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            return await db.Queryable<ContactRequestEntity>().In(contactRequestIds).ToListAsync();
        }



        public async Task<bool> ExistsPendingContactRequest(long senderId, long targetId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            return await db.Queryable<ContactRequestEntity>()
                .Where(c => c.Sender_Id == senderId && c.Receiver_Id == targetId && c.Relation_Request_Status == RelationRequestStatus.Pending)
                .AnyAsync();
        }
    }
}
