using HY.ApiService.Dtos;
using HY.ApiService.Entities;
using HY.ApiService.Enums;
using HY.ApiService.Models;
using HY.ApiService.Repositories;
using HY.ApiService.Tools;
using Mapster;
using SqlSugar;

namespace HY.ApiService.Services
{
    public interface IUserService
    {
        Task<long> CreateUser(RegisterRequest registerRequest);

        Task<UserDto?> GetUserById(long id);

        Task<bool> ExistsUsername(string username);
        Task<bool> ExistsEmail(string email);
        Task<bool> ExistsPhone(string phone);

        Task<bool> UpdateHead(long userId, string url);
    }

    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }




        public async Task<long> CreateUser(RegisterRequest registerRequest)
        {
            var hash = PBKDF2PasswordHasher.Hash(registerRequest.Password, out string salt);
            var hyid = Guid.NewGuid().ToString("N")[..16];

            var userEntity = new UserEntity
            {
                HYid = hyid,
                Username = registerRequest.Username,
                Nickname = registerRequest.Nickname,
                Avatar = null,
                Phone = registerRequest.Phone,
                Email = registerRequest.Email,
                Password_Hash = hash,
                Password_Salt = salt,
                Status = UserStatus.Registered,
                Created_At = DateTime.UtcNow
            };

            return await _userRepository.CreateUser(userEntity);
        }




        public async Task<UserDto?> GetUserById(long id)
        {
            var userEntity = await _userRepository.GetUserById(id);
            return userEntity?.Adapt<UserDto>();
        }




        public async Task<bool> ExistsUsername(string username)
        {
            return await _userRepository.ExistsUsername(username);
        }

        public async Task<bool> ExistsEmail(string email)
        {
            return await _userRepository.ExistsEmail(email);
        }

        public async Task<bool> ExistsPhone(string phone)
        {
            return await _userRepository.ExistsPhone(phone);
        }




        public async Task<bool> UpdateHead(long userId, string url)
        {
            return await _userRepository.UpdateHead(userId, url);
        }

    }
}
