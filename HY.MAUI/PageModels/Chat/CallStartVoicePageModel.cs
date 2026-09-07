using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HY.MAUI.Communication.SignalR;
using HY.MAUI.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.PageModels.Chat
{
    public partial class CallStartVoicePageModel : ObservableObject, IQueryAttributable
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


        public CallStartVoicePageModel(ChatHubSignalR chatHub)
        {
            _chatHub = chatHub;
        }


        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            _callId = query["CallId"]?.ToString();
            TargetAvatar = query["TargetAvatar"]?.ToString();
            TargetName = query["TargetName"]?.ToString();
        }


        [RelayCommand]
        async Task Appearing()
        {
        }

        [RelayCommand]
        void Disappearing()
        {
        }

        [RelayCommand]
        async Task HangUp()
        {

        }

    }
}
