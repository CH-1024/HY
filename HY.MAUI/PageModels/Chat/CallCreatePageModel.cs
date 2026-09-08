using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HY.MAUI.Communication;
using HY.MAUI.Communication.SignalR;
using HY.MAUI.Communication.SignalR.Requests;
using HY.MAUI.Enums;
using HY.MAUI.Models;
using HY.MAUI.Pages.Chat;
using HY.MAUI.Stores;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.PageModels.Chat
{
    public partial class CallCreatePageModel : ObservableObject, IQueryAttributable
    {
        private readonly ChatHubSignalR _chatHub;


        private string calleeAvatar;
        public string CalleeAvatar
        {
            get { return calleeAvatar; }
            set { SetProperty(ref calleeAvatar, value); }
        }

        private string calleeName;
        public string CalleeName
        {
            get { return calleeName; }
            set { SetProperty(ref calleeName, value); }
        }

        CreateCallRequest _createCallRequest;
        string _callId;




        public CallCreatePageModel(ChatHubSignalR chatHub)
        {
            _chatHub = chatHub;
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            _createCallRequest = (CreateCallRequest)query["CreateCallRequest"];
            CalleeAvatar = query["CalleeAvatar"]?.ToString();
            CalleeName = query["CalleeName"]?.ToString();
        }

        private async Task<bool> OnCallAccepted_ChatHub(string callId)
        {
            if (callId == _callId)
            {
                var parameters = new Dictionary<string, object>
                {
                    { "CallId", callId },
                    { "TargetAvatar", CalleeAvatar },
                    { "TargetName", CalleeName },
                };

                if (_createCallRequest.CallType == CallType.Video)
                {
                    await Shell.Current.GoToAsync($"../{nameof(CallStartVideoPage)}", false, parameters);
                }
                else if (_createCallRequest.CallType == CallType.Voice)
                {
                    await Shell.Current.GoToAsync($"../{nameof(CallStartVoicePage)}", false, parameters);
                }

                return true;
            }
            return false;
        }

        private async void OnCallRejected_ChatHub(string callId)
        {
            if (callId == _callId)
            {
                _ = Application.Current!.Windows[0].Page!.DisplayAlertAsync("提示", "对方拒绝", "退出");
                await Shell.Current.GoToAsync("..", false);
            }
        }

        private async void OnCallExpiry_ChatHub(string callId)
        {
            if (callId == _callId)
            {
                _ = Application.Current!.Windows[0].Page!.DisplayAlertAsync("提示", "请求超时", "退出");
                await Shell.Current.GoToAsync("..", false);
            }
        }


        [RelayCommand]
        async Task Appearing()
        {
            var resp = await _chatHub.CreateCall(_createCallRequest);
            if (resp.IsSucc)
            {
                _callId = resp.GetValue<string>("CallId");

                _chatHub.OnCallAccepted_ChatHub += OnCallAccepted_ChatHub;
                _chatHub.OnCallRejected_ChatHub += OnCallRejected_ChatHub;
                _chatHub.OnCallExpiry_ChatHub += OnCallExpiry_ChatHub;
            }
            else
            {
                _ = Application.Current!.Windows[0].Page!.DisplayAlertAsync("提示", $"请求失败[{resp?.Msg}]", "退出");
                await Shell.Current.GoToAsync("..", false);
            }
        }

        [RelayCommand]
        void Disappearing()
        {
            _chatHub.OnCallAccepted_ChatHub -= OnCallAccepted_ChatHub;
            _chatHub.OnCallRejected_ChatHub -= OnCallRejected_ChatHub;
            _chatHub.OnCallExpiry_ChatHub -= OnCallExpiry_ChatHub;
        }

        [RelayCommand]
        async Task Cancel()
        {
            await _chatHub.CancelCall(_callId);
            await Shell.Current.GoToAsync("..", false);
        }



    }
}
