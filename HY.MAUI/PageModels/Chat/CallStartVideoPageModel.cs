using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HY.MAUI.Communication.RTC;
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
        readonly IGlobalCache _globalCache;
        readonly ChatHubSignalR _chatHub;
        readonly ChatStore _chatStore;


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

        private Brush statusColor = Colors.Gray;
        public Brush StatusColor
        {
            get { return statusColor; }
            set { SetProperty(ref statusColor, value); }
        }

        bool _isCaller;
        WebRTCService _webRTC;
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
            _webRTC = (WebRTCService)query["WebRTC"];
            _isCaller = (bool)query["IsCaller"];
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

        private async void OnConnectionStateChanged_WebRTC(SIPSorcery.Net.RTCPeerConnectionState state)
        {
            await _chatHub.CallStateChanged(_callId, state.ToString());

            if (state == SIPSorcery.Net.RTCPeerConnectionState.connected)
            {
                StatusColor = Colors.LightGreen;
            }
            else if (state == SIPSorcery.Net.RTCPeerConnectionState.connecting)
            {
                StatusColor = Colors.Orange;
            }
            else if (state == SIPSorcery.Net.RTCPeerConnectionState.disconnected)
            {
                StatusColor = Colors.Red;
            }
            else if (state == SIPSorcery.Net.RTCPeerConnectionState.closed)
            {
                StatusColor = Colors.Gray;
            }
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

            _webRTC.OnConnectionStateChanged += OnConnectionStateChanged_WebRTC;
            _webRTC.OnReceivedMessage += OnReceivedMessage_WebRTC;

            if (_isCaller)
            {
                await _webRTC.SendOffer();
            }
        }


        [RelayCommand]
        void Disappearing()
        {
            _timer.Stop();
            _timer.Tick -= DispatcherTimer_Tick;

            _chatHub.OnCallAbnormal_ChatHub -= OnCallAbnormal_ChatHub;
            _chatHub.OnCallHangUp_ChatHub -= OnCallHangUp_ChatHub;
            _chatHub.OnReceiveMessage_ChatHub -= OnReceiveMessage_ChatHub;

            _webRTC.OnConnectionStateChanged -= OnConnectionStateChanged_WebRTC;
            _webRTC.OnReceivedMessage -= OnReceivedMessage_WebRTC;
            _webRTC.Dispose();
            _webRTC = null;
        }

        [RelayCommand]
        async Task HangUp()
        {
            var resp = await _chatHub.HangUpCall(_callId);
            if (resp.IsSucc) await Shell.Current.GoToAsync("..", false);
        }




        private void OnReceivedMessage_WebRTC(byte[] obj)
        {
            try
            {
                Message = Encoding.UTF8.GetString(obj);
            }
            catch (Exception)
            {
                Message = "无法解析消息内容";
            }
        }

        private string text;
        public string Text
        {
            get { return text; }
            set { SetProperty(ref text, value); }
        }
        private string message;
        public string Message
        {
            get { return message; }
            set { SetProperty(ref message, value); }
        }

        [RelayCommand]
        void Send()
        {
            _webRTC.SendMessage(Text);
        }
    }
}
