using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HY.MAUI.Communication.Http;
using HY.MAUI.Models;
using HY.MAUI.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.PageModels.Contact
{
    public partial class StrangerDetailPageModel : ObservableObject, IQueryAttributable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IGlobalCache _globalCache;
        private readonly ContactApi _contactApi;



        private ContactVM strangerInfo = null;
        public ContactVM StrangerInfo
        {
            get { return strangerInfo; }
            set { SetProperty(ref strangerInfo, value); }
        }



        public StrangerDetailPageModel(IServiceProvider serviceProvider, IGlobalCache globalCache, ContactApi contactApi)
        {
            _serviceProvider = serviceProvider;
            _globalCache = globalCache;
            _contactApi = contactApi;
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            StrangerInfo = (ContactVM)query["ContactInfo"];
        }

        [RelayCommand]
        async Task GoBack()
        {
            await Shell.Current.GoToAsync("..", true);
        }


        [RelayCommand]
        async Task AddContact()
        {
            var resp = await _contactApi.AddContact(StrangerInfo.HYid);
            if (resp?.IsSucc == true)
            {
                await Shell.Current.DisplayAlertAsync("Success", "Friend request sent successfully.", "OK");
            }
            else
            {
                await Shell.Current.DisplayAlertAsync("Error", $"Failed to send friend request: {resp?.Msg ?? "Unknown error"}", "OK");
            }
        }

    }

}
