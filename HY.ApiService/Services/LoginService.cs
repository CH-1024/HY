using HY.ApiService.Dtos;
using HY.ApiService.Entities;
using HY.ApiService.Enums;
using HY.ApiService.Models;
using HY.ApiService.Repositories;
using HY.ApiService.Tools;
using Mapster;

namespace HY.ApiService.Services
{
    public interface ILoginService
    {
        Task<RefreshResult> Refresh(RefreshRequest param);
        Task<LoginResult> Login(string ip, LoginRequest param);
        Task<bool> Logout(long userId, string deviceId);

        Task<bool> UpdateLoginDeviceOnline(long userId, string deviceId, bool isOnline);
    }


    public class LoginService : ILoginService
    {
        private readonly IUserService _userService;
        private readonly IRedisTokenService _redisTokenService;

        private readonly ILoginTokenRepository _loginTokenRepository;
        private readonly ILoginDeviceRepository _loginDeviceRepository;


        public LoginService(IUserService userService, IRedisTokenService redisTokenService, ILoginTokenRepository loginTokenRepository, ILoginDeviceRepository loginDeviceRepository)
        {
            _userService = userService;
            _redisTokenService = redisTokenService;

            _loginTokenRepository = loginTokenRepository;
            _loginDeviceRepository = loginDeviceRepository;
        }



        public async Task<RefreshResult> Refresh(RefreshRequest param)
        {
            var loginTokenEntity = await _loginTokenRepository.GetValidityLoginTokenByDeviceIdAndRefreshToken(param.DeviceId, param.RefreshToken);
            if (loginTokenEntity == null)
            {
                return new RefreshResult(false, "无效的刷新请求!");
            }

            var user = await _userService.GetUserById(loginTokenEntity.User_Id);
            if (user == null)
            {
                return new RefreshResult(false, "用户不存在!");
            }
            else if (user.Status == UserStatus.Banned)
            {
                return new RefreshResult(false, "用户已被禁用!");
            }
            else if (user.Status == UserStatus.Deactivated)
            {
                return new RefreshResult(false, "用户已停用!");
            }
            else if (user.Status == UserStatus.Deleted)
            {
                return new RefreshResult(false, "用户已注销!");
            }

            var newRefreshToken = loginTokenEntity.Refresh_Token;

            if (loginTokenEntity.Refresh_Expired < DateTime.UtcNow.AddDays(7))
            {
                newRefreshToken = TokenGenerator.GenerateRefreshToken();

                loginTokenEntity.Refresh_Token = newRefreshToken;
                loginTokenEntity.Refresh_Expired = DateTime.UtcNow.AddDays(20);

                await _loginTokenRepository.UpdateLoginToken(loginTokenEntity);
            }

            var newAccessToken = TokenGenerator.GenerateAccessToken(user.Id, user.HYid, param.DeviceId, param.DevicePlatform, out DateTime accessExpires);
            await _redisTokenService.SaveAsync(user.Id, param.DeviceId, newAccessToken, accessExpires);

            var tokenResult = new TokenResult(newAccessToken, newRefreshToken, accessExpires);

            return new RefreshResult(true, string.Empty, tokenResult);
        }

        public async Task<LoginResult> Login(string ip, LoginRequest param)
        {
            var user = await _userService.Login(param.Username, param.Password);
            if (user == null)
            {
                return new LoginResult(false, "用户名或密码错误!");
            }
            else if (user.Status == UserStatus.Banned)
            {
                return new LoginResult(false, "用户已被禁用!");
            }
            else if (user.Status == UserStatus.Deactivated)
            {
                return new LoginResult(false, "用户已停用!");
            }
            else if (user.Status == UserStatus.Deleted)
            {
                return new LoginResult(false, "用户已注销!");
            }

            #region 更新LoginToken

            var refreshToken = TokenGenerator.GenerateRefreshToken();

            var loginTokenEntity = await _loginTokenRepository.GetLoginTokenByUserIdAndDeviceId(user.Id, param.DeviceId);
            if (loginTokenEntity == null)
            {
                loginTokenEntity = new LoginTokenEntity
                {
                    User_Id = user.Id,
                    Device_Id = param.DeviceId,
                    Refresh_Token = refreshToken,
                    Refresh_Expired = DateTime.UtcNow.AddDays(30),
                    Revoked = false,
                    Created_At = DateTime.UtcNow,
                };

                await _loginTokenRepository.CreateLoginToken(loginTokenEntity);
            }
            else
            {
                loginTokenEntity.Refresh_Token = refreshToken;
                loginTokenEntity.Refresh_Expired = DateTime.UtcNow.AddDays(30);
                loginTokenEntity.Revoked = false;

                await _loginTokenRepository.UpdateLoginToken(loginTokenEntity);
            }

            var accessToken = TokenGenerator.GenerateAccessToken(user.Id, user.HYid, param.DeviceId, param.DevicePlatform, out DateTime accessExpires);
            await _redisTokenService.SaveAsync(user.Id, param.DeviceId, accessToken, accessExpires);

            var tokenResult = new TokenResult(accessToken, refreshToken, accessExpires);

            #endregion

            #region 更新LoginDevice

            var loginingDevice = await _loginDeviceRepository.GetLoginDeviceByUserIdAndDeviceId(user.Id, param.DeviceId);
            if (loginingDevice == null)
            {
                var loginDeviceEntity = new LoginDeviceEntity
                {
                    User_Id = user.Id,
                    Device_Id = param.DeviceId,
                    Device_Platform = param.DevicePlatform,
                    Device_Type = param.DeviceType,
                    Device_Name = param.DeviceName,
                    Last_Login_Ip = ip,
                    Last_Login_At = DateTime.UtcNow,
                    //Is_Online = true,                   // IsOnline只在ChatHub实时更新
                };

                await _loginDeviceRepository.CreateLoginDevice(loginDeviceEntity);
            }
            else
            {
                loginingDevice.Device_Platform = param.DevicePlatform;
                loginingDevice.Device_Type = param.DeviceType;
                loginingDevice.Device_Name = param.DeviceName;
                loginingDevice.Last_Login_Ip = ip;
                loginingDevice.Last_Login_At = DateTime.UtcNow;
                //loginingDevice.Is_Online = true;       // IsOnline只在ChatHub实时更新

                await _loginDeviceRepository.UpdateLoginDevice(loginingDevice);
            }

            var logedInDevices = await _loginDeviceRepository.GetLoginDeviceByUserIdAndDevicePlatformAndIsOnline(user.Id, param.DevicePlatform);
            foreach (var logedInDevice in logedInDevices)
            {
                // Todo: 下线处理
                logedInDevice.Is_Online = false;
                await _loginDeviceRepository.UpdateLoginDevice(logedInDevice);
            }

            #endregion

            return new LoginResult(true, string.Empty, user, tokenResult);
        }

        public async Task<bool> Logout(long userId, string deviceId)
        {
            // 删除Redis中的Token
            await _redisTokenService.RemoveAsync(userId, deviceId);
            // 更新登录令牌状态
            return await _loginTokenRepository.UpdateLoginTokenRevoked(userId, deviceId);
        }

        public async Task<bool> UpdateLoginDeviceOnline(long userId, string deviceId, bool isOnline)
        {
            return await _loginDeviceRepository.UpdateLoginDeviceOnline(userId, deviceId, isOnline);
        }

    }
}
