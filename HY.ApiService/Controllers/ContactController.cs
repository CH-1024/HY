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
        [HttpGet("get/contactrequests")]
        public async Task<IActionResult> GetContactRequests()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var userId = long.Parse(userIdStr!);

            var result = await _contactService.GetAllContactRequestsByUserId(userId);

            return Ok(new Response(result.IsSucc, result.Error)
            {
                Data = new Dictionary<string, object?>
                {
                    { "ContactRequests", result.ContactRequests },
                }
            });
        }

        [Authorize]
        [HttpGet("get/contacts")]
        public async Task<IActionResult> GetContacts()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var userId = long.Parse(userIdStr!);

            var result = await _contactService.GetAllContactsByUserId(userId);

            return Ok(new Response(result.IsSucc, result.Error)
            {
                Data = new Dictionary<string, object?>
                {
                    { "Contacts", result.Contacts },
                }
            });
        }

        [Authorize]
        [HttpGet("get/contact")]
        public async Task<IActionResult> GetContact(long targetId)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var userId = long.Parse(userIdStr!);

            var result = await _contactService.GetContactByUserId(userId, targetId);

            return Ok(new Response(result.IsSucc, result.Error)
            {
                Data = new Dictionary<string, object?>
                {
                    { "Contact", result.Contact },
                }
            });
        }

        [Authorize]
        [HttpGet("search/contact")]
        public async Task<IActionResult> SearchContact(string identity)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var userId = long.Parse(userIdStr!);

            var result = await _contactService.GetContactByHYidOrPhone(userId, identity);

            return Ok(new Response(result.IsSucc, result.Error)
            {
                Data = new Dictionary<string, object?>
                {
                    { "Contact", result.Contact },
                }
            });
        }

        [Authorize]
        [HttpDelete("delete/contact")]
        public async Task<IActionResult> DeleteContact(long targetId)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var userId = long.Parse(userIdStr!);

            var result = await _contactService.DeleteContact(userId, targetId);

            return Ok(new Response(result.IsSucc, result.Error));
        }

    }
}
