using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HY.MAUI.Communication;
using HY.MAUI.Communication.Http;
using HY.MAUI.Communication.RTC;
using HY.MAUI.Communication.SignalR;
using HY.MAUI.Communication.SignalR.Requests;
using HY.MAUI.Enums;
using HY.MAUI.Pages.Chat;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.PageModels.Chat
{
    public partial class CallSelectPageModel : ObservableObject, IQueryAttributable
    {
        readonly ChatHubSignalR _chatHub;
        readonly CallApi _callApi;


        private string callerAvatar;
        public string CallerAvatar
        {
            get { return callerAvatar; }
            set { SetProperty(ref callerAvatar, value); }
        }

        private string callerName;
        public string CallerName
        {
            get { return callerName; }
            set { SetProperty(ref callerName, value); }
        }

        ReceiveCallRequest _receiveCallRequest;



        public CallSelectPageModel(ChatHubSignalR chatHub, CallApi callApi)
        {
            _chatHub = chatHub;
            _callApi = callApi;
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            _receiveCallRequest = (ReceiveCallRequest)query["ReceiveCallRequest"];
            CallerAvatar = query["CallerAvatar"]?.ToString();
            CallerName = query["CallerName"]?.ToString();
        }

        private async void OnCallAccepted_ChatHub(string callId, DateTime start)
        {
            if (callId == _receiveCallRequest.CallId)
            {
                var _webRTC = new WebRTCService();
                await _webRTC.Initialize(_receiveCallRequest.CallId);

                var parameters = new Dictionary<string, object>
                {
                    { "WebRTC", _webRTC },
                    { "IsCaller", false },
                    { "CallId", _receiveCallRequest.CallId },
                    { "TargetAvatar", CallerAvatar },
                    { "TargetName", CallerName },
                    { "StartAt", start},
                };

                if (_receiveCallRequest.CallType == CallType.Video)
                {
                    await Shell.Current.GoToAsync($"../{nameof(CallStartVideoPage)}", false, parameters);
                }
                else if (_receiveCallRequest.CallType == CallType.Voice)
                {
                    await Shell.Current.GoToAsync($"../{nameof(CallStartVoicePage)}", false, parameters);
                }
            }
        }

        private async void OnCallCanceled_ChatHub(string callId)
        {
            if (callId == _receiveCallRequest.CallId)
            {
                _ = Application.Current!.Windows[0].Page!.DisplayAlertAsync("提示", "对方取消", "退出");
                await Shell.Current.GoToAsync("..", false);
            }
        }

        private async void OnCallHandled_ChatHub(string callId)
        {
            if (callId == _receiveCallRequest.CallId)
            {
                //_ = Application.Current!.Windows[0].Page!.DisplayAlertAsync("提示", "通话已在其他设备处理", "退出");
                await Shell.Current.GoToAsync("..", false);
            }
        }

        private async void OnCallExpiry_ChatHub(string callId)
        {
            if (callId == _receiveCallRequest.CallId)
            {
                _ = Application.Current!.Windows[0].Page!.DisplayAlertAsync("提示", "请求超时", "退出");
                await Shell.Current.GoToAsync("..", false);
            }
        }


        

        [RelayCommand]
        void Appearing()
        {
            _chatHub.OnCallAccepted_ChatHub += OnCallAccepted_ChatHub;
            _chatHub.OnCallCanceled_ChatHub += OnCallCanceled_ChatHub;
            _chatHub.OnCallHandled_ChatHub += OnCallHandled_ChatHub;
            _chatHub.OnCallExpiry_ChatHub += OnCallExpiry_ChatHub;
        }

        [RelayCommand]
        void Disappearing()
        {
            _chatHub.OnCallAccepted_ChatHub -= OnCallAccepted_ChatHub;
            _chatHub.OnCallCanceled_ChatHub -= OnCallCanceled_ChatHub;
            _chatHub.OnCallHandled_ChatHub -= OnCallHandled_ChatHub;
            _chatHub.OnCallExpiry_ChatHub -= OnCallExpiry_ChatHub;
        }

        [RelayCommand]
        async Task Accept()
        {
            var resp = await _callApi.AcceptCall(_receiveCallRequest.CallId);
            if (!resp.IsSucc) _ = Application.Current!.Windows[0].Page!.DisplayAlertAsync("提示", $"接听异常:{resp.Msg}", "退出");
        }

        [RelayCommand]
        async Task Reject()
        {
            var resp = await _callApi.RejectCall(_receiveCallRequest.CallId);
            if (resp.IsSucc) await Shell.Current.GoToAsync("..", false);
        }
    }
}
