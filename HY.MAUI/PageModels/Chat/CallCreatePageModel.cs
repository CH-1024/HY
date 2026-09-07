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
        CancellationTokenSource _globalCts;
        //CancellationToken linkedToken1 = CancellationTokenSource.CreateLinkedTokenSource(globalCts.Token, new CancellationTokenSource().Token).Token;

        IDispatcherTimer _timer;


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
        DateTime _expiry;




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
            if (callId == _callId && !_globalCts.IsCancellationRequested)
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
            if (callId == _callId && !_globalCts.IsCancellationRequested)
            {
                _ = Application.Current!.Windows[0].Page!.DisplayAlertAsync("提示", "对方拒绝", "退出");
                await Shell.Current.GoToAsync("..", false);
            }
        }

        private async void DispatcherTimer_Tick(object? sender, EventArgs e)
        {
            await _globalCts.CancelAsync();

            _ = Application.Current!.Windows[0].Page!.DisplayAlertAsync("提示", "请求超时", "退出");
            await Shell.Current.GoToAsync("..", false);
        }


        [RelayCommand]
        async Task Appearing()
        {
            _globalCts = new CancellationTokenSource();
            _timer = Dispatcher.GetForCurrentThread()!.CreateTimer();

            var resp = await _chatHub.CreateCall(_createCallRequest, _globalCts.Token);
            if (resp.IsSucc)
            {
                _callId = resp.GetValue<string>("CallId");
                _expiry = resp.GetValue<DateTime>("ExpiryAt");

                _timer.Interval = _expiry - DateTime.UtcNow;
                _timer.IsRepeating = false;
                _timer.Tick += DispatcherTimer_Tick;
                _timer.Start();

                _chatHub.OnCallAccepted_ChatHub += OnCallAccepted_ChatHub;
                _chatHub.OnCallRejected_ChatHub += OnCallRejected_ChatHub;
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
            _timer.Stop();
            _timer.Tick -= DispatcherTimer_Tick;

            _chatHub.OnCallAccepted_ChatHub -= OnCallAccepted_ChatHub;
            _chatHub.OnCallRejected_ChatHub -= OnCallRejected_ChatHub;
        }

        [RelayCommand]
        async Task Cancel()
        {
            await _chatHub.CancelCall(_callId, _globalCts.Token);
            await Shell.Current.GoToAsync("..", false);
        }



    }
}
