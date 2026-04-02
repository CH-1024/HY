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
        string? _source = null;

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
            _source = (string)query["Source"];
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
            await _chatHub.RequestContact(StrangerInfo.Contact_Id.Value, _source, "Hi, let's be friends!");
        }

    }

}
