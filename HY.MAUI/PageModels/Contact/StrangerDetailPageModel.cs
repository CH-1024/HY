using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HY.MAUI.Communication.Http;
using HY.MAUI.Communication.SignalR;
using HY.MAUI.Models;
using HY.MAUI.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.PageModels.Contact
{
    public partial class StrangerDetailPageModel : ObservableObject, IQueryAttributable
    {
        int _source = 0;

        private readonly IServiceProvider _serviceProvider;
        private readonly IGlobalCache _globalCache;
        private readonly ContactApi _contactApi;

        private readonly ChatHubSignalR _chatHub;

        private ContactVM strangerInfo = null;
        public ContactVM StrangerInfo
        {
            get { return strangerInfo; }
            set { SetProperty(ref strangerInfo, value); }
        }



        public StrangerDetailPageModel(IServiceProvider serviceProvider, IGlobalCache globalCache, ContactApi contactApi, ChatHubSignalR chatHub)
        {
            _serviceProvider = serviceProvider;
            _globalCache = globalCache;
            _contactApi = contactApi;

            _chatHub = chatHub;
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            _source = (int)query["Source"];
            StrangerInfo = (ContactVM)query["ContactInfo"];
        }

        [RelayCommand]
        async Task GoBack()
        {
            await Shell.Current.GoToAsync("..", true);
        }


        [RelayCommand]
        async Task RequestContact()
        {
            var currentUser = _globalCache.GetCurrentUser();

            if (currentUser.Id == StrangerInfo.Contact_Id)
            {
                _ = Application.Current!.Windows[0].Page!.DisplayAlertAsync("提示", "这是你自己哦！", "确定");
                return;
            }

            var sayHi = await Application.Current!.Windows[0].Page!.DisplayPromptAsync("发送好友请求", null, "确定", "取消", "打个招呼吧~", 30);
            if (sayHi != null)
            {
                await _chatHub.RequestContact(StrangerInfo.Contact_Id, _source, sayHi);
                _ = Application.Current!.Windows[0].Page!.DisplayAlertAsync("提示", "请求已发送", "确定");
            }

        }

    }

}
