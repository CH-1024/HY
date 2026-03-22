using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HY.MAUI.Communication;
using HY.MAUI.Communication.Http;
using HY.MAUI.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.PageModels.Contact
{
    public partial class SearchContactPageModel : ObservableObject
    {


        private readonly IServiceProvider _serviceProvider;
        private readonly IGlobalCache _globalCache;
        private readonly ContactApi _contactApi;


        public SearchContactPageModel(IServiceProvider serviceProvider, IGlobalCache globalCache, ContactApi contactApi)
        {
            _serviceProvider = serviceProvider;
            _globalCache = globalCache;
            _contactApi = contactApi;

        }


        [RelayCommand]
        async Task SearchContact(string identity)
        {
            var response = _contactApi.SearchContact(identity);
        }

    }
}
