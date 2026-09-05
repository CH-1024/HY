using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HY.MAUI.Communication;
using HY.MAUI.Communication.SignalR;
using HY.MAUI.Enums;
using HY.MAUI.Models;
using HY.MAUI.Pages.Chat;
using HY.MAUI.Stores;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.PageModels.Chat
{
    public partial class CallWaitPageModel : ObservableObject, IQueryAttributable
    {
        CancellationTokenSource globalCts;
        //CancellationToken linkedToken1 = CancellationTokenSource.CreateLinkedTokenSource(globalCts.Token, new CancellationTokenSource().Token).Token;

        IDispatcherTimer _timeoutTimer = Dispatcher.GetForCurrentThread()!.CreateTimer();


        private readonly ChatHubSignalR _chatHub;


        private string? targetAvatar;
        public string? TargetAvatar
        {
            get { return targetAvatar; }
            set { SetProperty(ref targetAvatar, value); }
        }

        private string? targetName;
        public string? TargetName
        {
            get { return targetName; }
            set { SetProperty(ref targetName, value); }
        }

        CallType _callType;
        ChatType _chatType;
        long _calleeId;




        public CallWaitPageModel(ChatHubSignalR chatHub)
        {
            _chatHub = chatHub;
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            _callType = (CallType)query["CallType"];
            _chatType = (ChatType)query["ChatType"];
            _calleeId = Convert.ToInt64(query["CalleeId"]);
            TargetAvatar = query["TargetAvatar"]?.ToString();
            TargetName = query["TargetName"]?.ToString();
        }

        private async void OnAcceptCall_ChatHub(CallType callType, ChatType chatType, long calleeId)
        {
            if (callType == _callType && chatType == _chatType && calleeId == _calleeId && !globalCts.IsCancellationRequested)
                await Shell.Current.GoToAsync($"../{nameof(CallBeginPage)}", false);
        }

        private async void OnRejectCall_ChatHub(CallType callType, ChatType chatType, long calleeId)
        {
            if (callType == _callType && chatType == _chatType && calleeId == _calleeId && !globalCts.IsCancellationRequested)
            {
                _ = Application.Current!.Windows[0].Page!.DisplayAlertAsync("提示", "对方拒绝", "退出");
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

            _timeoutTimer.Interval = TimeSpan.FromSeconds(3600);
            _timeoutTimer.IsRepeating = false;
            _timeoutTimer.Tick += DispatcherTimer_Tick;
            _timeoutTimer.Start();

            var resp = await _chatHub.CreateCall(_callType, _chatType, _calleeId, globalCts.Token);
            if (resp.IsSucc)
            {
                _chatHub.OnAcceptCall_ChatHub += OnAcceptCall_ChatHub;
                _chatHub.OnRejectCall_ChatHub += OnRejectCall_ChatHub;
            }
            else if (globalCts.IsCancellationRequested)
            {
                // 防止因网卡导致后退两步
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
            _timeoutTimer.Stop();
            _timeoutTimer.Tick -= DispatcherTimer_Tick;

            _chatHub.OnAcceptCall_ChatHub -= OnAcceptCall_ChatHub;
            _chatHub.OnRejectCall_ChatHub -= OnRejectCall_ChatHub;
        }

        [RelayCommand]
        async Task Cancel()
        {
            await _chatHub.CancelCall(_callType, _chatType, _calleeId, globalCts.Token);
            await Shell.Current.GoToAsync("..", false);
        }



    }
}
