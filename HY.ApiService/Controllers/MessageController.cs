using HY.ApiService.Enums;
using HY.ApiService.Models;
using HY.ApiService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SqlSugar;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace HY.ApiService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MessageController : ControllerBase
    {
        readonly IChatService _chatService;
        readonly IMessageService _messageService;


        public MessageController(IChatService chatService, IMessageService messageService)
        {
            _chatService = chatService;
            _messageService = messageService;
        }



        [Authorize]
        [HttpGet("get/messages")]
        public async Task<IActionResult> GetMessages(long chatId, long skipMessageId, int take)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var userId = long.Parse(userIdStr!);

            var bol = await _chatService.IsUserOwnerChat(userId, chatId);
            if (!bol)
            {
                return Ok(new Response(false, "没有权限"));
            }

            var messages = await _messageService.GetMessagesByChatId(chatId, skipMessageId, take);

            return Ok(new Response(true)
            {
                Data = new Dictionary<string, object?> 
                {
                    { "Messages",  messages}
                }
            });
        }


        //[Authorize]
        //[HttpGet("get/single/messages")]
        //public async Task<IActionResult> GetSingleChatMessages(long userId1, long userId2, int skip, int take)
        //{
        //    var messages = await _messageService.GetPrivateChatMessages(userId1, userId2, skip, take);
        //    return Ok(new Response(true)
        //    {
        //        Data = new Dictionary<string, object?>
        //        {
        //            { "Messages",  messages}
        //        }
        //    });
        //}


        //[Authorize]
        //[HttpGet("get/group/messages")]
        //public async Task<IActionResult> GetGroupChatMessages(long groupId, int skip, int take)
        //{
        //    var messages = await _messageService.GetGroupChatMessages(groupId, skip, take);
        //    return Ok(new Response(true)
        //    {
        //        Data = new Dictionary<string, object?>
        //        {
        //            { "Messages",  messages}
        //        }
        //    });
        //}

    }
}
