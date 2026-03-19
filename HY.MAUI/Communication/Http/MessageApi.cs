using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace HY.MAUI.Communication.Http
{
    public class MessageApi : BaseApi
    {
        public MessageApi(HttpClient http) : base(http)
        {

        }


        public async Task<Response?> GetMessages(long chatId, long skipMessageId = 0, int take = 50)
        {
            return await GetAsync($"{ApiUrl.GetMessages}?chatId={chatId}&skipMessageId={skipMessageId}&take={take}");
        }

    }
}
