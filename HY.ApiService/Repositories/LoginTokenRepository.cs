
using HY.ApiService.Dtos;
using HY.ApiService.Entities;
using SqlSugar;

namespace HY.ApiService.Repositories
{
    public interface ILoginTokenRepository
    {
        Task<long> CreateLoginToken(LoginTokenEntity loginToken);

        Task<LoginTokenEntity?> GetLoginTokenByUserIdAndDeviceId(long userId, string deviceId);
        Task<LoginTokenEntity?> GetLoginTokenByDeviceIdAndRefreshToken(string deviceId, string refreshToken);
        Task<LoginTokenEntity?> GetValidityLoginTokenByDeviceIdAndRefreshToken(string deviceId, string refreshToken);

        Task<bool> UpdateLoginToken(LoginTokenEntity loginTokenEntity);
        Task<bool> UpdateLoginTokenRevoked(long userId, string deviceId);
    }


    public class LoginTokenRepository : ILoginTokenRepository
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public LoginTokenRepository(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }


        public async Task<long> CreateLoginToken(LoginTokenEntity loginToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            loginToken.Id = await db.Insertable(loginToken).ExecuteReturnBigIdentityAsync();
            return loginToken.Id;
        }



        public async Task<LoginTokenEntity?> GetLoginTokenByUserIdAndDeviceId(long userId, string deviceId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            return await db.Queryable<LoginTokenEntity>()
                .Where(lt => lt.User_Id == userId && lt.Device_Id == deviceId)
                .SingleAsync();
        }

        public async Task<LoginTokenEntity?> GetLoginTokenByDeviceIdAndRefreshToken(string deviceId, string refreshToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            return await db.Queryable<LoginTokenEntity>()
                .Where(lt => lt.Device_Id == deviceId && lt.Refresh_Token == refreshToken)
                .SingleAsync();
        }

        public async Task<LoginTokenEntity?> GetValidityLoginTokenByDeviceIdAndRefreshToken(string deviceId, string refreshToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            return await db.Queryable<LoginTokenEntity>()
                .Where(lt => lt.Device_Id == deviceId && lt.Refresh_Token == refreshToken && !lt.Revoked && lt.Refresh_Expired > DateTime.UtcNow)
                .SingleAsync();
        }




        public async Task<bool> UpdateLoginToken(LoginTokenEntity loginTokenEntity)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var result = await db.Updateable(loginTokenEntity).ExecuteCommandAsync();
            return result > 0;
        }

        public async Task<bool> UpdateLoginTokenRevoked(long userId, string deviceId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var result = await db.Updateable<LoginTokenEntity>()
                .SetColumns(lt => lt.Revoked == true)
                .Where(lt => lt.User_Id == userId && lt.Device_Id == deviceId)
                .ExecuteCommandAsync();
            return result > 0;
        }

    }
}
