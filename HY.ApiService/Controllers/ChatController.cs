using HY.ApiService.Dtos;
using HY.ApiService.Models;
using HY.ApiService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HY.ApiService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ChatController : ControllerBase
    {
        readonly IChatService _chatService;


        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }



        [Authorize]
        [HttpGet("get/chats")]
        public async Task<IActionResult> GetChats()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var userId = long.Parse(userIdStr!);

            var list = await _chatService.GetAllChatsByUserId(userId);

            return Ok(new Response(true)
            {
                Data = new Dictionary<string, object?>
                {
                    { "Chats",  list }
                }
            });
        }


        [Authorize]
        [HttpPost("read/all")]
        public async Task<IActionResult> ReadAll(long chatId)
        {
            var result = await _chatService.UpdateChatUnread(chatId);
            return Ok(new Response(result));
        }


    }
}
