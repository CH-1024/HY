
using HY.ApiService.Entities;
using SqlSugar;

namespace HY.ApiService.Repositories
{
    public interface IContactRepository
    {
        Task<List<ContactEntity>> GetContactsByUserId(long userId);
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


    }
}
