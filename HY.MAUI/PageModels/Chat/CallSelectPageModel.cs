using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HY.MAUI.Communication;
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

        private ReceiveCallRequest _receiveCallRequest;

        public CallSelectPageModel(ChatHubSignalR chatHub)
        {
            _chatHub = chatHub;
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            _receiveCallRequest = (ReceiveCallRequest)query["ReceiveCallRequest"];
            CallerAvatar = query["CallerAvatar"]?.ToString();
            CallerName = query["CallerName"]?.ToString();
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
        async Task Appearing()
        {
            _chatHub.OnCallCanceled_ChatHub += OnCallCanceled_ChatHub;
            _chatHub.OnCallHandled_ChatHub += OnCallHandled_ChatHub;
            _chatHub.OnCallExpiry_ChatHub += OnCallExpiry_ChatHub;
        }

        [RelayCommand]
        void Disappearing()
        {
            _chatHub.OnCallCanceled_ChatHub -= OnCallCanceled_ChatHub;
            _chatHub.OnCallHandled_ChatHub -= OnCallHandled_ChatHub;
            _chatHub.OnCallExpiry_ChatHub -= OnCallExpiry_ChatHub;
        }

        [RelayCommand]
        async Task Accept()
        {
            var resp = await _chatHub.AcceptCall(_receiveCallRequest.CallId);
            if (resp.IsSucc)
            {
                var parameters = new Dictionary<string, object>
                {
                    { "CallId", _receiveCallRequest.CallId },
                    { "TargetAvatar", CallerAvatar },
                    { "TargetName", CallerName },
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

        [RelayCommand]
        async Task Reject()
        {
            await _chatHub.RejectCall(_receiveCallRequest.CallId);
            await Shell.Current.GoToAsync("..");
        }
    }
}
