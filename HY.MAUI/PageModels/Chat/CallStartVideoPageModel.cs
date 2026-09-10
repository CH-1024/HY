using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HY.MAUI.Communication.SignalR;
using HY.MAUI.Dtos;
using HY.MAUI.Enums;
using HY.MAUI.Mapping;
using HY.MAUI.Models.MsgVM;
using HY.MAUI.Services.Interfaces;
using HY.MAUI.Stores;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace HY.MAUI.PageModels.Chat
{
    public partial class CallStartVideoPageModel : ObservableObject, IQueryAttributable
    {
        private readonly IGlobalCache _globalCache;

        private readonly ChatHubSignalR _chatHub;

        private readonly ChatStore _chatStore;

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

        private TimeSpan startTime;
        public TimeSpan StartTime
        {
            get { return startTime; }
            set { SetProperty(ref startTime, value); }
        }



        string _callId;
        DateTime _startAt;


        public CallStartVideoPageModel(IGlobalCache globalCache, ChatHubSignalR chatHub, ChatStore chatStore)
        {
            _globalCache = globalCache;
            _chatHub = chatHub;
            _chatStore = chatStore;
        }


        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            _callId = query["CallId"]?.ToString();
            _startAt = (DateTime)query["StartAt"];
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

        private Task<bool> OnReceiveMessage_ChatHub(MessageDto msgDto)
        {
            var currentUser = _globalCache.GetCurrentUser();
            var chat = _chatStore.GetChat(currentUser.Id, msgDto);
            if (chat != null)
            {
                var messageVM = msgDto.ToVM(currentUser.Id);
                if (messageVM is CallVideoMessageVM videoCallMsg && videoCallMsg.CallId == _callId)
                {
                    if (!messageVM.IsSelf)
                    {
                        if (chat.Unread_Count > 0)
                        {
                            chat.Unread_Count -= 1;
                        }
                    }

                    return Task.FromResult(true);
                }
            }
            return Task.FromResult(false);
        }

        private void DispatcherTimer_Tick(object? sender, EventArgs e)
        {
            StartTime = DateTime.UtcNow - _startAt;
        }


        IDispatcherTimer _timer;

        [RelayCommand]
        async Task Appearing()
        {
            _timer = Application.Current!.Dispatcher.CreateTimer();

            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.IsRepeating = true;
            _timer.Tick += DispatcherTimer_Tick;
            _timer.Start();

            _chatHub.OnCallAbnormal_ChatHub += OnCallAbnormal_ChatHub;
            _chatHub.OnCallHangUp_ChatHub += OnCallHangUp_ChatHub;
            _chatHub.OnReceiveMessage_ChatHub += OnReceiveMessage_ChatHub;
        }


        [RelayCommand]
        void Disappearing()
        {
            _timer.Stop();
            _timer.Tick -= DispatcherTimer_Tick;

            _chatHub.OnCallAbnormal_ChatHub -= OnCallAbnormal_ChatHub;
            _chatHub.OnCallHangUp_ChatHub -= OnCallHangUp_ChatHub;
            _chatHub.OnReceiveMessage_ChatHub -= OnReceiveMessage_ChatHub;
        }

        [RelayCommand]
        async Task HangUp()
        {
            var resp = await _chatHub.HangUpCall(_callId);
            if (resp.IsSucc) await Shell.Current.GoToAsync("..", false);
        }

    }
}
