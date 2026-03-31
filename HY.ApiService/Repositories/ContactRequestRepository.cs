using HY.ApiService.Entities;
using SqlSugar;

namespace HY.ApiService.Repositories
{
    public interface IContactRequestRepository
    {
        Task<ContactRequestEntity> GetContactRequestById(long contact_Request_Id);
        Task<List<ContactRequestEntity>> GetContactRequestsByIds(List<long> contactRequestIds);
    }


    public class ContactRequestRepository : IContactRequestRepository
    {
        private readonly IServiceScopeFactory _scopeFactory;


        public ContactRequestRepository(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
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

    }
}
