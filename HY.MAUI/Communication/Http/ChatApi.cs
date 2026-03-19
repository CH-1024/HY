using HY.MAUI.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace HY.MAUI.Communication.Http
{
    public class ChatApi : BaseApi
    {
        public ChatApi(HttpClient http) : base(http)
        {

        }


        public async Task<Response?> GetChats()
        {
            return await GetAsync(ApiUrl.GetChats);
        }

        public async Task<Response?> ReadAll(long chatId)
        {
            return await GetAsync($"{ApiUrl.ReadAll}?chatId={chatId}");
        }

    }
}
