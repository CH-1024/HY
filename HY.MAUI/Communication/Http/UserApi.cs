using HY.MAUI.Communication.Requests;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace HY.MAUI.Communication.Http
{
    public class UserApi : BaseApi
    {
        public UserApi(HttpClient http) : base(http)
        {

        }


        public async Task<Response?> Register(string nickname, string username, string password, string email, string phone)
        {
            var param = new RegisterRequest
            {
                Nickname = nickname,
                Username = username,
                Password = password,
                Email = email,
                Phone = phone
            };

            return await PostAsJsonAsync(ApiUrl.Register, param);
        }

        public async Task<Response?> UpdateHead(string url)
        {
            var param = new UpdateUserRequest
            {
                Avatar = url,
            };

            return await PatchAsJsonAsync(ApiUrl.UpdateHead, param);
        }
    }
}
