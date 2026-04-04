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
        [HttpPatch("update/head")]
        public async Task<IActionResult> UpdateHead([FromBody] UpdateUserRequest updateUserRequest)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var userId = long.Parse(userIdStr!);

            var result = await _userService.UpdateHead(userId, updateUserRequest.Avatar);

            return Ok(new Response(result));
        }
    }
}
