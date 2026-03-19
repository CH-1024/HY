
using HY.ApiService.Entities;
using SqlSugar;

namespace HY.ApiService.Repositories
{
    public interface ILoginDeviceRepository
    {
        Task<long> CreateLoginDevice(LoginDeviceEntity loginDevice);

        Task<LoginDeviceEntity?> GetLoginDeviceById(long id);
        Task<List<LoginDeviceEntity>> GetLoginDeviceByUserIdAndDevicePlatformAndIsOnline(long userId, string devicePlatform);
        Task<LoginDeviceEntity?> GetLoginDeviceByUserIdAndDeviceId(long userId, string deviceId);

        Task<bool> UpdateLoginDevice(LoginDeviceEntity loginDevice);
        Task<bool> UpdateLoginDeviceOnline(long userId, string deviceId, bool isOnline);
    }


    public class LoginDeviceRepository : ILoginDeviceRepository
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public LoginDeviceRepository(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }




        public async Task<long> CreateLoginDevice(LoginDeviceEntity loginDevice)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            loginDevice.Id = await db.Insertable(loginDevice).ExecuteReturnBigIdentityAsync();
            return loginDevice.Id;
        }




        public async Task<LoginDeviceEntity?> GetLoginDeviceById(long id)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            return await db.Queryable<LoginDeviceEntity>()
                .Where(ld => ld.Id == id)
                .SingleAsync();
        }

        public async Task<List<LoginDeviceEntity>> GetLoginDeviceByUserIdAndDevicePlatformAndIsOnline(long userId, string devicePlatform)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            return await db.Queryable<LoginDeviceEntity>()
                .Where(ld => ld.User_Id == userId && ld.Device_Platform == devicePlatform && ld.Is_Online)
                .ToListAsync();
        }

        public async Task<LoginDeviceEntity?> GetLoginDeviceByUserIdAndDeviceId(long userId, string deviceId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            return await db.Queryable<LoginDeviceEntity>()
                .Where(ld => ld.User_Id == userId && ld.Device_Id == deviceId)
                .SingleAsync();
        }




        public async Task<bool> UpdateLoginDevice(LoginDeviceEntity loginDevice)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var result = await db.Updateable(loginDevice).ExecuteCommandAsync();
            return result > 0;
        }

        public async Task<bool> UpdateLoginDeviceOnline(long userId, string deviceId, bool isOnline)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var result = await db.Updateable<LoginDeviceEntity>()
                .SetColumns(ld => ld.Is_Online == isOnline)
                .Where(ld => ld.User_Id == userId && ld.Device_Id == deviceId)
                .ExecuteCommandAsync();
            return result > 0;
        }

    }
}
