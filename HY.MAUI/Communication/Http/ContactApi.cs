using HY.MAUI.Communication.Requests;
using HY.MAUI.Models;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace HY.MAUI.Communication.Http
{
    public class ContactApi : BaseApi
    {
        public ContactApi(HttpClient http) : base(http)
        {

        }


        public async Task<Response?> GetContacts()
        {
            return await GetAsync(ApiUrl.GetContacts);
        }

        public async Task<Response?> GetContact(long targetId)
        {
            return await GetAsync($"{ApiUrl.GetContact}?targetId={targetId}");
        }

        public async Task<Response?> SearchContact(string identity)
        {
            return await GetAsync($"{ApiUrl.SearchContact}?identity={identity}");
        }

        public async Task<Response?> RequestContact(string hyid, string msg)
        {
            var param = new ContactRequest
            {
                HYid = hyid,
                Message = msg
            };

            return await PostAsJsonAsync(ApiUrl.RequestContact, param);
        }
    }

}
