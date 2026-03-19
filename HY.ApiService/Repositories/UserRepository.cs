using HY.ApiService.Dtos;
using HY.ApiService.Entities;
using HY.ApiService.Models;
using HY.ApiService.Services;
using HY.ApiService.Tools;
using SqlSugar;

namespace HY.ApiService.Repositories
{
    public interface IUserRepository
    {
        Task<long> CreateUser(UserEntity user);

        Task<UserEntity?> GetUserById(long id);
        Task<UserEntity?> GetUserByUsername(string username);
        Task<List<UserEntity>> GetUsersByIds(List<long> userIds);

        Task<bool> ExistsUsername(string username);
        Task<bool> ExistsEmail(string email);
        Task<bool> ExistsPhone(string phone);

        Task<bool> UpdateHead(long userId, string url);
    }

    public class UserRepository : IUserRepository
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public UserRepository(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }






        //public void SomeDatabaseOperation()
        //{
        //    using var scope = _scopeFactory.CreateScope();
        //    var db = scope.ServiceProvider.GetRequiredService<SqlSugar.ISqlSugarClient>();
        //    // 在这里使用 db 进行数据库操作
        //}

        //public async Task ProcessBatchAsync(List<int> ids)
        //{
        //    var tasks = ids.Select(async id =>
        //    {
        //        using var scope = _scopeFactory.CreateScope();
        //        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        //        // 同一个 scope 内 db 上下文是同一个对象
        //        await userRepository.GetUserById(id);
        //        await userRepository.GetUserById(id);
        //    });

        //    await Task.WhenAll(tasks);
        //}

        public async Task<long> CreateUser(UserEntity user)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            user.Id = await db.Insertable(user).ExecuteReturnBigIdentityAsync();
            return user.Id;
        }





        public async Task<UserEntity?> GetUserById(long id)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            return await db.Queryable<UserEntity>().InSingleAsync(id);
        }

        public async Task<UserEntity?> GetUserByUsername(string username)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            return await db.Queryable<UserEntity>().Where(u => u.Username == username).SingleAsync();
        }

        public async Task<List<UserEntity>> GetUsersByIds(List<long> userIds)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            return await db.Queryable<UserEntity>().In(userIds).ToListAsync();
        }




        public async Task<bool> ExistsUsername(string username)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            return await db.Queryable<UserEntity>().AnyAsync(u => u.Username == username);
        }

        public async Task<bool> ExistsEmail(string email)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            return await db.Queryable<UserEntity>().AnyAsync(u => u.Email == email);
        }

        public async Task<bool> ExistsPhone(string phone)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            return await db.Queryable<UserEntity>().AnyAsync(u => u.Phone == phone);
        }




        public async Task<bool> UpdateHead(long userId, string url)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var result = await db.Updateable<UserEntity>().SetColumns(u => u.Avatar == url).Where(u => u.Id == userId).ExecuteCommandAsync();
            return result > 0;
        }

    }
}
