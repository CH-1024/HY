using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HY.MAUI.Communication.SignalR;
using HY.MAUI.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.PageModels.Chat
{
    public partial class CallChoosePageModel : ObservableObject, IQueryAttributable
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

        public CallChoosePageModel(ChatHubSignalR chatHub)
        {
            _chatHub = chatHub;
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            _callType = (CallType)query["CallType"];
            _chatType = (ChatType)query["ChatType"];
            _callerId = Convert.ToInt64(query["CallerId"]);
            CallerAvatar = query["CallerAvatar"]?.ToString();
            CallerName = query["CallerName"]?.ToString();
        }


        private async void OnCancelCall_ChatHub(CallType callType, ChatType chatType, long callerId)
        {
            if (callType == _callType && chatType == _chatType && callerId == _callerId && !globalCts.IsCancellationRequested)
            {
                _ = Application.Current!.Windows[0].Page!.DisplayAlertAsync("提示", "对方取消", "退出");
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

            _timeoutTimer.Interval = TimeSpan.FromSeconds(30);
            _timeoutTimer.IsRepeating = false;
            _timeoutTimer.Tick += DispatcherTimer_Tick;
            _timeoutTimer.Start();

            _chatHub.OnCancelCall_ChatHub += OnCancelCall_ChatHub;
        }

        [RelayCommand]
        void Disappearing()
        {
            _timeoutTimer.Stop();
            _timeoutTimer.Tick -= DispatcherTimer_Tick;

            _chatHub.OnCancelCall_ChatHub -= OnCancelCall_ChatHub;
        }

        [RelayCommand]
        async Task Accept()
        {
            await _chatHub.AcceptCall(_callType, _chatType, _callerId, globalCts.Token);
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        async Task Reject()
        {
            await _chatHub.RejectCall(_callType, _chatType, _callerId, globalCts.Token);
            await Shell.Current.GoToAsync("..");
        }
    }
}
