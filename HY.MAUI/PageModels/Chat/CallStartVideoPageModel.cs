using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HY.MAUI.Communication.SignalR;
using HY.MAUI.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.PageModels.Chat
{
    public partial class CallStartVideoPageModel : ObservableObject, IQueryAttributable
    {
        readonly ChatHubSignalR _chatHub;

        private string targetAvatar;
        public string TargetAvatar
        {
            get { return targetAvatar; }
            set { SetProperty(ref targetAvatar, value); }
        }

        private string targetName;
        public string TargetName
        {
            get { return targetName; }
            set { SetProperty(ref targetName, value); }
        }

        string _callId;


        public CallStartVideoPageModel(ChatHubSignalR chatHub)
        {
            _chatHub = chatHub;
        }


        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            _callId = query["CallId"]?.ToString();
            TargetAvatar = query["TargetAvatar"]?.ToString();
            TargetName = query["TargetName"]?.ToString();
        }

        private async void OnCallAbnormal_ChatHub(string callId)
        {
            if (callId == _callId)
            {
                _ = Application.Current!.Windows[0].Page!.DisplayAlertAsync("提示", "通话异常断开", "退出");
                await Shell.Current.GoToAsync("..", false);
            }
        }

        private async void OnCallHangUp_ChatHub(string callId)
        {
            if (callId == _callId)
            {
                //_ = Application.Current!.Windows[0].Page!.DisplayAlertAsync("提示", "通话已在其他设备处理", "退出");
                await Shell.Current.GoToAsync("..", false);
            }
        }


        [RelayCommand]
        async Task Appearing()
        {
            _chatHub.OnCallAbnormal_ChatHub += OnCallAbnormal_ChatHub;
            _chatHub.OnCallHangUp_ChatHub += OnCallHangUp_ChatHub;
        }


        [RelayCommand]
        void Disappearing()
        {
            _chatHub.OnCallAbnormal_ChatHub -= OnCallAbnormal_ChatHub;
            _chatHub.OnCallHangUp_ChatHub -= OnCallHangUp_ChatHub;
        }

        [RelayCommand]
        async Task HangUp()
        {
            await _chatHub.HangUpCall(_callId, CancellationToken.None);
            await Shell.Current.GoToAsync("..", false);
        }

    }
}
