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
    public partial class CallCreatePageModel : ObservableObject, IQueryAttributable
    {
        CancellationTokenSource globalCts;
        //CancellationToken linkedToken1 = CancellationTokenSource.CreateLinkedTokenSource(globalCts.Token, new CancellationTokenSource().Token).Token;

        IDispatcherTimer _timeoutTimer = Dispatcher.GetForCurrentThread()!.CreateTimer();


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

        CallType _callType;
        ChatType _chatType;
        long _calleeId;
        DateTime _expiry;




        public CallCreatePageModel(ChatHubSignalR chatHub)
        {
            _chatHub = chatHub;
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            _callType = (CallType)query["CallType"];
            _chatType = (ChatType)query["ChatType"];
            _calleeId = Convert.ToInt64(query["CalleeId"]);
            CalleeAvatar = query["CalleeAvatar"]?.ToString();
            CalleeName = query["CalleeName"]?.ToString();
        }

        private bool OnAcceptCall_ChatHub(CallType callType, ChatType chatType, long calleeId)
        {
            if (callType == _callType && chatType == _chatType && calleeId == _calleeId && !globalCts.IsCancellationRequested)
            {
                var parameters = new Dictionary<string, object>
                {
                    { "ChatType", _chatType },
                    { "CalleeId", _calleeId },
                    { "TargetAvatar", CalleeAvatar },
                    { "TargetName", CalleeName },
                };

                if (callType == CallType.Video)
                {
                    _ = Shell.Current.GoToAsync($"../{nameof(CallStartVideoPage)}", false, parameters);
                }
                else if (callType == CallType.Voice)
                {
                    _ = Shell.Current.GoToAsync($"../{nameof(CallStartVoicePage)}", false, parameters);
                }

                return true;
            }
            return false;
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

            var now = DateTime.UtcNow;
            var expiry =  now.AddSeconds(3600);
            _expiry = expiry;

            _timeoutTimer.Interval = expiry - now;
            _timeoutTimer.IsRepeating = false;
            _timeoutTimer.Tick += DispatcherTimer_Tick;
            _timeoutTimer.Start();

            var resp = await _chatHub.CreateCall(_callType, _chatType, _calleeId, _expiry, globalCts.Token);
            if (resp.IsSucc)
            {
                _chatHub.OnAcceptCall_ChatHub += OnAcceptCall_ChatHub;
                _chatHub.OnRejectCall_ChatHub += OnRejectCall_ChatHub;
            }
            else if (globalCts.IsCancellationRequested)
            {
                // 防止因超时导致后退两步
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
            await _chatHub.CancelCall(_callType, _chatType, _calleeId, _expiry, globalCts.Token);
            await Shell.Current.GoToAsync("..", false);
        }



    }
}
