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
        Task<UserEntity?> GetUserByHYid(string hyid);
        Task<UserEntity?> GetUserByUsername(string username);
        Task<List<UserEntity>> GetUsersByIds(List<long> userIds);
        Task<List<UserEntity>> GetUserByHYidOrPhone(string identity);

        Task<bool> ExistsUsername(string username);
        Task<bool> ExistsEmail(string email);
        Task<bool> ExistsPhone(string phone);

        Task<bool> UpdateHead(long userId, string url);
    }

    public class UserRepository : IUserRepository
    {
        private readonly ISqlSugarClient _db;

        public UserRepository(ISqlSugarClient db)
        {
            _db = db;
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
            user.Id = await _db.Insertable(user).ExecuteReturnBigIdentityAsync();
            return user.Id;
        }





        public async Task<UserEntity?> GetUserById(long id)
        {
            return await _db.Queryable<UserEntity>().InSingleAsync(id);
        }

        public async Task<UserEntity?> GetUserByHYid(string hyid)
        {
            return await _db.Queryable<UserEntity>().Where(u => u.HYid == hyid).SingleAsync();
        }

        public async Task<UserEntity?> GetUserByUsername(string username)
        {
            return await _db.Queryable<UserEntity>().Where(u => u.Username == username).SingleAsync();
        }

        public async Task<List<UserEntity>> GetUsersByIds(List<long> userIds)
        {
            return await _db.Queryable<UserEntity>().In(userIds).ToListAsync();
        }

        public async Task<List<UserEntity>> GetUserByHYidOrPhone(string identity)
        {
            return await _db.Queryable<UserEntity>().Where(u => u.HYid == identity || u.Phone == identity).ToListAsync();
        }



        public async Task<bool> ExistsUsername(string username)
        {
            return await _db.Queryable<UserEntity>().AnyAsync(u => u.Username == username);
        }

        public async Task<bool> ExistsEmail(string email)
        {
            return await _db.Queryable<UserEntity>().AnyAsync(u => u.Email == email);
        }

        public async Task<bool> ExistsPhone(string phone)
        {
            return await _db.Queryable<UserEntity>().AnyAsync(u => u.Phone == phone);
        }




        public async Task<bool> UpdateHead(long userId, string url)
        {
            var result = await _db.Updateable<UserEntity>().SetColumns(u => u.Avatar == url).Where(u => u.Id == userId).ExecuteCommandAsync();
            return result > 0;
        }

    }
}
