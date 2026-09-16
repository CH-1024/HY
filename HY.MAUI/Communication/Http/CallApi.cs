using HY.MAUI.Communication.SignalR.Requests;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.Communication.Http
{
    public class CallApi : BaseApi
    {
        public CallApi(HttpClient http) : base(http)
        {

        }


        public async Task<Response?> CreateCall(CreateCallRequest request)
        {
            return await PostAsJsonAsync(ApiUrl.CreateCall, request);
        }

        public async Task<Response?> AcceptCall(string callId)
        {
            return await PostAsync($"{ApiUrl.AcceptCall}?callId={callId}");
        }

        public async Task<Response?> CancelCall(string callId)
        {
            return await PostAsync($"{ApiUrl.CancelCall}?callId={callId}");
        }

        public async Task<Response?> RejectCall(string callId)
        {
            return await PostAsync($"{ApiUrl.RejectCall}?callId={callId}");
        }

        public async Task<Response?> HangUpCall(string callId)
        {
            return await PostAsync($"{ApiUrl.HangUpCall}?callId={callId}");
        }

        public async Task<Response?> CallConnected(string callId)
        {
            return await PostAsync($"{ApiUrl.CallConnected}?callId={callId}");
        }

    }
}
