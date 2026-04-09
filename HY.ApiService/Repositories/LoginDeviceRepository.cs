
using HY.ApiService.Entities;
using SqlSugar;

namespace HY.ApiService.Repositories
{
    public interface ILoginDeviceRepository
    {
        Task<long> CreateLoginDevice(LoginDeviceEntity loginDevice);

        Task<LoginDeviceEntity?> GetLoginDeviceById(long id);
        Task<List<LoginDeviceEntity>> GetLoginDeviceByUserIdAndDevicePlatformAndIsOnline(long userId, int devicePlatform);
        Task<LoginDeviceEntity?> GetLoginDeviceByUserIdAndDeviceId(long userId, string deviceId);

        Task<bool> UpdateLoginDevice(LoginDeviceEntity loginDevice);
        Task<bool> UpdateLoginDeviceOnline(long userId, string deviceId, bool isOnline);
    }


    public class LoginDeviceRepository : ILoginDeviceRepository
    {
        private readonly ISqlSugarClient _db;

        public LoginDeviceRepository(ISqlSugarClient db)
        {
            _db = db;
        }




        public async Task<long> CreateLoginDevice(LoginDeviceEntity loginDevice)
        {
            loginDevice.Id = await _db.Insertable(loginDevice).ExecuteReturnBigIdentityAsync();
            return loginDevice.Id;
        }




        public async Task<LoginDeviceEntity?> GetLoginDeviceById(long id)
        {
            return await _db.Queryable<LoginDeviceEntity>()
                .Where(ld => ld.Id == id)
                .SingleAsync();
        }

        public async Task<List<LoginDeviceEntity>> GetLoginDeviceByUserIdAndDevicePlatformAndIsOnline(long userId, int devicePlatform)
        {
            return await _db.Queryable<LoginDeviceEntity>()
                .Where(ld => ld.User_Id == userId && ld.Device_Platform == devicePlatform && ld.Is_Online)
                .ToListAsync();
        }

        public async Task<LoginDeviceEntity?> GetLoginDeviceByUserIdAndDeviceId(long userId, string deviceId)
        {
            return await _db.Queryable<LoginDeviceEntity>()
                .Where(ld => ld.User_Id == userId && ld.Device_Id == deviceId)
                .SingleAsync();
        }




        public async Task<bool> UpdateLoginDevice(LoginDeviceEntity loginDevice)
        {
            var result = await _db.Updateable(loginDevice).ExecuteCommandAsync();
            return result > 0;
        }

        public async Task<bool> UpdateLoginDeviceOnline(long userId, string deviceId, bool isOnline)
        {
            var result = await _db.Updateable<LoginDeviceEntity>()
                .SetColumns(ld => ld.Is_Online == isOnline)
                .Where(ld => ld.User_Id == userId && ld.Device_Id == deviceId)
                .ExecuteCommandAsync();
            return result > 0;
        }

    }
}
