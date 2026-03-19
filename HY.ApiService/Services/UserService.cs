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
        Task<UserDto?> CreateUser(string nickname, string username, string password, string phone, string email);

        Task<UserDto?> Login(string username, string password);
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




        public async Task<UserDto?> CreateUser(string nickname, string username, string password, string phone, string email)
        {
            var hash = PBKDF2PasswordHasher.Hash(password, out string salt);
            var hyid = Guid.NewGuid().ToString("N")[..16];

            var userEntity = new UserEntity
            {
                HYid = hyid,
                Username = username,
                Nickname = nickname,
                Avatar = null,
                Phone = phone,
                Email = email,
                Password_Hash = hash,
                Password_Salt = salt,
                Status = UserStatus.Registered,
                Created_At = DateTime.UtcNow
            };

            var userId = await _userRepository.CreateUser(userEntity);
            if (userId == 0)
            {
                return null;
            }

            return userEntity.Adapt<UserDto>();
        }




        public async Task<UserDto?> Login(string username, string password)
        {
            var userEntity = await _userRepository.GetUserByUsername(username);
            if (userEntity == null)
            {
                return null;
            }
            bool isValid = PBKDF2PasswordHasher.Verify(password, userEntity.Password_Hash!, userEntity.Password_Salt!);
            if (!isValid)
            {
                return null;
            }

            return userEntity?.Adapt<UserDto>();
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
