using HY.ApiService.Controllers.Requests;
using HY.ApiService.Enums;
using HY.ApiService.Models;
using HY.ApiService.Services;
using HY.ApiService.Tools;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static System.Net.Mime.MediaTypeNames;

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
        public async Task<IActionResult> Register([FromBody] RegisterUserRequest registerRequest)
        {
            RegisterResult result;

            if (await _userService.ExistsUsername(registerRequest.Username))
            {
                result = new RegisterResult(false, "用户名已存在");
            }
            else if (await _userService.ExistsEmail(registerRequest.Email))
            {
                result = new RegisterResult(false, "邮箱已存在");
            }
            else if (await _userService.ExistsPhone(registerRequest.Phone))
            {
                result = new RegisterResult(false, "手机号已存在");
            }

            var id = await _userService.CreateUser(registerRequest);

            return Ok(new Response(id > 0));
        }

        [Authorize]
        [HttpPost("update/head")]
        public async Task<IActionResult> UpdateHead(string url)
        {
            var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            if (!RegexHelper.IsValidUrl(url))
            {
                return Ok(new Response(false, "头像 URL 无效"));
            }

            var result = await _userService.UpdateHead(userId, url);

            return Ok(new Response(result));
        }

        [Authorize]
        [HttpPost("update/profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserRequest updateUserRequest)
        {
            var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            if (!RegexHelper.IsValidNickname(updateUserRequest.Nickname))
            {
                return Ok(new Response(false, "昵称格式不正确"));
            }
            else if (!RegexHelper.IsValidPhone(updateUserRequest.Phone))
            {
                return Ok(new Response(false, "手机号格式不正确"));
            }
            else if (!RegexHelper.IsValidEmail(updateUserRequest.Email))
            {
                return Ok(new Response(false, "邮箱格式不正确"));
            }

            var result = await _userService.UpdateProfile(userId, updateUserRequest);

            return Ok(new Response(result));
        }
    }
}
