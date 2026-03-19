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
    public class ContactController : ControllerBase
    {
        readonly IContactService _contactService;


        public ContactController(IContactService contactService)
        {
            _contactService = contactService;
        }



        [Authorize]
        [HttpGet("get/contacts")]
        public async Task<IActionResult> GetContacts()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var userId = long.Parse(userIdStr!);

            var list = await _contactService.GetContactsByUserId(userId);

            return Ok(new Response(true)
            {
                Data = new Dictionary<string, object?>
                {
                    { "Contacts",  list }
                }
            });
        }


        [Authorize]
        [HttpGet("get/stranger")]
        public async Task<IActionResult> GetStranger(long strangerId)
        {
            var stranger = await _contactService.GetStrangerByUserId(strangerId);

            return Ok(new Response(true)
            {
                Data = new Dictionary<string, object?>
                {
                    { "Stranger",  stranger }
                }
            });
        }

    }

}
