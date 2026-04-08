
using HY.ApiService.Entities;
using HY.ApiService.Services;
using SqlSugar;

namespace HY.ApiService.Repositories
{
    public interface IContactRepository
    {
        Task<long> CreateContact(ContactEntity contactEntity);

        Task<List<ContactEntity>> GetContactsByUserId(long userId);
        Task<ContactEntity?> GetUserContactByUserId(long currentUserId, long contactId);
        Task<List<ContactEntity>> GetUserContactByUserIds(long currentUserId, List<long> contactIds);

        Task<bool> UpdateContact(ContactEntity contactEntity);
    }


    public class ContactRepository : IContactRepository
    {
        private readonly ISqlSugarClient _db;

        public ContactRepository(ISqlSugarClient db)
        {
            _db = db;
        }



        public async Task<long> CreateContact(ContactEntity contactEntity)
        {
            return await _db.Insertable(contactEntity).ExecuteCommandAsync();
        }



        public async Task<List<ContactEntity>> GetContactsByUserId(long userId)
        {
            return await _db.Queryable<ContactEntity>()
                .Where(c => c.User_Id == userId)
                .ToListAsync();
        }

        public async Task<ContactEntity?> GetUserContactByUserId(long currentUserId, long contactId)
        {
            return await _db.Queryable<ContactEntity>()
                .Where(c => c.User_Id == currentUserId && c.Contact_Id == contactId)
                .SingleAsync();
        }

        public async Task<List<ContactEntity>> GetUserContactByUserIds(long currentUserId, List<long> contactIds)
        {
            return await _db.Queryable<ContactEntity>()
                .Where(c => c.User_Id == currentUserId && contactIds.Contains(c.Contact_Id))
                .ToListAsync();
        }



        public async Task<bool> UpdateContact(ContactEntity contactEntity)
        {
            return await _db.Updateable(contactEntity).ExecuteCommandAsync() > 0;
        }

    }
}
