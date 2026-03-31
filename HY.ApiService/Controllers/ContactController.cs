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
        [HttpGet("get/contact")]
        public async Task<IActionResult> GetContact(long targetId)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var userId = long.Parse(userIdStr!);

            var contact = await _contactService.GetContactByUserId(userId, targetId);

            return Ok(new Response(true)
            {
                Data = new Dictionary<string, object?>
                {
                    { "Contact",  contact }
                }
            });
        }

        [Authorize]
        [HttpGet("search/contact")]
        public async Task<IActionResult> SearchContact(string identity)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var userId = long.Parse(userIdStr!);

            var contacts = await _contactService.GetContactByHYidOrPhone(userId, identity);

            return Ok(new Response(true)
            {
                Data = new Dictionary<string, object?>
                {
                    { "Contacts",  contacts }
                }
            });
        }

        [Authorize]
        [HttpPost("request/contact")]
        public async Task<IActionResult> RequestContact(ContactRequest contactRequest)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var userId = long.Parse(userIdStr!);

            await _contactService.RequestContact(userId, contactRequest.HYid);

            return Ok(new Response(false));
        }

        [Authorize]
        [HttpPost("response/contact")]
        public async Task<IActionResult> ResponseContact(ContactRequest contactRequest)
        {
            var userIdStr = User.FindFirst("HYid")?.Value;

            var userId = long.Parse(userIdStr!);

            await _contactService.ResponseContact(userId, contactRequest.HYid);

            return Ok(new Response(false));
        }

    }

}
