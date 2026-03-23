
using HY.ApiService.Entities;
using SqlSugar;

namespace HY.ApiService.Repositories
{
    public interface IContactRepository
    {
        Task<List<ContactEntity>> GetContactsByUserId(long userId);
        Task<ContactEntity?> GetUserContactByUserId(long currentUserId, long contactId);
        Task<List<ContactEntity>> GetUserContactByUserIds(long currentUserId, List<long> contactIds);
    }


    public class ContactRepository : IContactRepository
    {
        private readonly IServiceScopeFactory _scopeFactory;


        public ContactRepository(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }



        public async Task<List<ContactEntity>> GetContactsByUserId(long userId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            return await db.Queryable<ContactEntity>()
                .Where(c => c.User_Id == userId)
                .ToListAsync();
        }

        public async Task<ContactEntity?> GetUserContactByUserId(long currentUserId, long contactId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            return await db.Queryable<ContactEntity>()
                .Where(c => c.User_Id == currentUserId && c.Contact_Id == contactId)
                .SingleAsync();
        }

        public async Task<List<ContactEntity>> GetUserContactByUserIds(long currentUserId, List<long> contactIds)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            return await db.Queryable<ContactEntity>()
                .Where(c => c.User_Id == currentUserId && contactIds.Contains(c.Contact_Id))
                .ToListAsync();



        }
    }
}
