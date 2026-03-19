using HY.ApiService.Enums;
using HY.ApiService.Models;
using HY.ApiService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HY.ApiService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        readonly IUserService _userService;


        public UserController(IUserService userService)
        {
            _userService = userService;
        }


        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest registerRequest)
        {
            if (await _userService.ExistsUsername(registerRequest.Username))
            {
                return Ok(new Response(false, "用户名已存在"));
            }
            if (await _userService.ExistsEmail(registerRequest.Email))
            {
                return Ok(new Response(false, "邮箱已存在"));
            }
            if (await _userService.ExistsPhone(registerRequest.Phone))
            {
                return Ok(new Response(false, "手机号已存在"));
            }

            var nickname = registerRequest.Nickname;
            var username = registerRequest.Username;
            var password = registerRequest.Password;
            var phone = registerRequest.Phone;
            var email = registerRequest.Email;
            await _userService.CreateUser(nickname, username, password, phone, email);

            return Ok(new Response(true));
        }

        [Authorize]
        [HttpPatch("update/head")]
        public async Task<IActionResult> UpdateHead([FromBody] UpdateUserRequest updateUserRequest)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdStr, out var userId)) return Unauthorized();

            var result = await _userService.UpdateHead(userId, updateUserRequest.Avatar);
            return Ok(new Response(result));
        }
    }
}
