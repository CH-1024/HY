using HY.ApiService.Dtos;

namespace HY.ApiService.Models
{
    public class ContactResult
    {
        public bool IsSucc { get; init; }
        public string? Error { get; init; }
        public ContactDto? Contact { get; init; }
        public List<ContactDto>? Contacts { get; init; }
        public ContactRequestDto? ContactRequest { get; init; }
        public List<ContactRequestDto>? ContactRequests { get; init; }

        public ContactResult(bool isSucc, string? error = null, ContactDto? contact = null, List<ContactDto>? contacts = null, ContactRequestDto? contactRequest = null, List<ContactRequestDto>? contactRequests = null)
        {
            IsSucc = isSucc;
            Error = error;

            Contact = contact;
            Contacts = contacts;

            ContactRequest = contactRequest;
            ContactRequests = contactRequests;
        }

    }
}
