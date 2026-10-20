using HY.ApiService.Controllers.Requests;
using HY.ApiService.Dtos;
using HY.ApiService.Entities;
using HY.ApiService.Enums;
using HY.ApiService.Repositories;
using HY.ApiService.Tools;
using Mapster;
using SqlSugar;

namespace HY.ApiService.Services
{
    public interface IUserService
    {
        Task<long> CreateUser(RegisterUserRequest registerUserRequest);

        Task<UserDto?> GetUserById(long id);

        Task<bool> ExistsUsername(string username);
        Task<bool> ExistsEmail(string email);
        Task<bool> ExistsPhone(string phone);

        Task<bool> UpdateHead(long userId, string url);
        Task<bool> UpdateProfile(long userId, UpdateUserRequest updateUserRequest);
    }

    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }




        public async Task<long> CreateUser(RegisterUserRequest registerUserRequest)
        {
            var hash = PBKDF2PasswordHasher.Hash(registerUserRequest.Password, out string salt);
            var hyid = Guid.NewGuid().ToString("N")[..16];

            var userEntity = new UserEntity
            {
                HYid = hyid,
                Username = registerUserRequest.Username,
                Nickname = registerUserRequest.Nickname,
                Avatar = null,
                Phone = registerUserRequest.Phone,
                Email = registerUserRequest.Email,
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
            var userEntity = await _userRepository.GetUserById(userId);
            if (userEntity == null)
            {
                return false;
            }

            userEntity.Avatar = url;

            return await _userRepository.Update(userEntity);
        }

        public async Task<bool> UpdateProfile(long userId, UpdateUserRequest updateUserRequest)
        {
            var userEntity = await _userRepository.GetUserById(userId);
            if (userEntity == null)
            {
                return false;
            }

            var emailExists = await _userRepository.ExistsEmail(updateUserRequest.Email);
            if (emailExists && userEntity.Email != updateUserRequest.Email)
            {
                throw new Exception("Email already exists.");
            }

            var phoneExists = await _userRepository.ExistsPhone(updateUserRequest.Phone);
            if (phoneExists && userEntity.Phone != updateUserRequest.Phone)
            {
                throw new Exception("Phone number already exists.");
            }

            userEntity.Nickname = updateUserRequest.Nickname ?? userEntity.Nickname;
            userEntity.Phone = updateUserRequest.Phone ?? userEntity.Phone;
            userEntity.Email = updateUserRequest.Email ?? userEntity.Email;

            return await _userRepository.Update(userEntity);
        }
    }
}
