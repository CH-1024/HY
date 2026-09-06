using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HY.MAUI.Communication;
using HY.MAUI.Communication.SignalR;
using HY.MAUI.Enums;
using HY.MAUI.Pages.Chat;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.PageModels.Chat
{
    public partial class CallSelectPageModel : ObservableObject, IQueryAttributable
    {
        CancellationTokenSource globalCts;
        //CancellationToken linkedToken1 = CancellationTokenSource.CreateLinkedTokenSource(globalCts.Token, new CancellationTokenSource().Token).Token;

        IDispatcherTimer _timeoutTimer = Dispatcher.GetForCurrentThread()!.CreateTimer();


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

        private CallType _callType;
        private ChatType _chatType;
        private long _callerId;
        private DateTime _expiry;
        private int _callerPlatform;

        public CallSelectPageModel(ChatHubSignalR chatHub)
        {
            _chatHub = chatHub;
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            _callType = (CallType)query["CallType"];
            _chatType = (ChatType)query["ChatType"];
            _callerId = Convert.ToInt64(query["CallerId"]);
            _expiry = Convert.ToDateTime(query["Expiry"]);
            _callerPlatform = Convert.ToInt32(query["CallerPlatform"]);
            CallerAvatar = query["CallerAvatar"]?.ToString();
            CallerName = query["CallerName"]?.ToString();
        }


        private async void OnCancelCall_ChatHub(CallType callType, ChatType chatType, long callerId, int callerPlatform)
        {
            if (callType == _callType && chatType == _chatType && callerId == _callerId && _callerPlatform == callerPlatform && !globalCts.IsCancellationRequested)
            {
                _ = Application.Current!.Windows[0].Page!.DisplayAlertAsync("提示", "对方取消", "退出");
                await Shell.Current.GoToAsync("..", false);
            }
        }

        private async void OnCallHandled_ChatHub(CallType callType, ChatType chatType, long callerId)
        {
            if (callType == _callType && chatType == _chatType && callerId == _callerId && !globalCts.IsCancellationRequested)
            {
                //_ = Application.Current!.Windows[0].Page!.DisplayAlertAsync("提示", "通话已在其他设备处理", "退出");
                await Shell.Current.GoToAsync("..", false);
            }
        }

        private async void DispatcherTimer_Tick(object? sender, EventArgs e)
        {
            await globalCts.CancelAsync();

            _ = Application.Current!.Windows[0].Page!.DisplayAlertAsync("提示", "请求超时", "退出");
            await Shell.Current.GoToAsync("..", false);
        }


        [RelayCommand]
        async Task Appearing()
        {
            globalCts = new CancellationTokenSource();

            var now = DateTime.UtcNow;
            var expiry = _expiry;

            _timeoutTimer.Interval = expiry - now;
            _timeoutTimer.IsRepeating = false;
            _timeoutTimer.Tick += DispatcherTimer_Tick;
            _timeoutTimer.Start();

            _chatHub.OnCancelCall_ChatHub += OnCancelCall_ChatHub;
            _chatHub.OnCallHandled_ChatHub += OnCallHandled_ChatHub;
        }

        [RelayCommand]
        void Disappearing()
        {
            _timeoutTimer.Stop();
            _timeoutTimer.Tick -= DispatcherTimer_Tick;

            _chatHub.OnCancelCall_ChatHub -= OnCancelCall_ChatHub;
            _chatHub.OnCallHandled_ChatHub -= OnCallHandled_ChatHub;
        }

        [RelayCommand]
        async Task Accept()
        {
            if (_expiry <= DateTime.UtcNow)
            {
                _ = Application.Current!.Windows[0].Page!.DisplayAlertAsync("提示", "呼叫已过期", "退出");
                return;
            }

            var resp = await _chatHub.AcceptCall(_callType, _chatType, _callerId, _expiry, _callerPlatform, globalCts.Token);
            if (resp.IsSucc)
            {
                var parameters = new Dictionary<string, object>
                {
                    { "ChatType", _chatType },
                    { "CalleeId", _callerId },
                    { "TargetAvatar", CallerAvatar },
                    { "TargetName", CallerName },
                };

                if (_callType == CallType.Video)
                {
                    _ = Shell.Current.GoToAsync($"../{nameof(VideoCallStartPage)}", false, parameters);
                }
                else if (_callType == CallType.Voice)
                {
                    _ = Shell.Current.GoToAsync($"../{nameof(VoiceCallStartPage)}", false, parameters);
                }
            }
        }

        [RelayCommand]
        async Task Reject()
        {
            if (_expiry <= DateTime.UtcNow)
            {
                _ = Application.Current!.Windows[0].Page!.DisplayAlertAsync("提示", "呼叫已过期", "退出");
                return;
            }

            await _chatHub.RejectCall(_callType, _chatType, _callerId, _expiry, _callerPlatform, globalCts.Token);
            await Shell.Current.GoToAsync("..");
        }
    }
}
