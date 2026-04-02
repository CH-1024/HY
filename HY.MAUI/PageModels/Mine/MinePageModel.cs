using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HY.MAUI.Communication.Auth;
using HY.MAUI.Communication.Http;
using HY.MAUI.Communication.SignalR;
using HY.MAUI.Models;
using HY.MAUI.Pages.Login;
using HY.MAUI.Pages.Mine;
using HY.MAUI.Services;
using HY.MAUI.Services.Interfaces;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.PageModels.Mine
{
    public partial class MinePageModel : ObservableObject
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IGlobalCache _globalCache;
        private readonly ILoginService _loginService;
        private readonly ITokenProvider _tokenProvider;

        private readonly LoginApi _loginApi;
        private readonly ChatHubSignalR _chatHub;

        private UserVM currentUser = null;
        public UserVM CurrentUser
        {
            get { return currentUser; }
            set { SetProperty(ref currentUser, value); }
        }

        public MinePageModel(IServiceProvider serviceProvider, IGlobalCache globalCache, ILoginService loginService, ITokenProvider tokenProvider, LoginApi loginApi, ChatHubSignalR chatHub)
        {
            _serviceProvider = serviceProvider;
            _globalCache = globalCache;
            _loginService = loginService;
            _tokenProvider = tokenProvider;
            _loginApi = loginApi;
            _chatHub = chatHub;
        }

        [RelayCommand]
        void Appearing()
        {
            CurrentUser = _globalCache.GetCurrentUser();
        }




        [RelayCommand]
        async Task Profile()
        {
            await Shell.Current.GoToAsync(nameof(ProfilePage), true);
        }


        [RelayCommand]
        async Task Logout()
        {
            var confirm = await Application.Current!.Windows[0].Page!.DisplayAlertAsync("Confirm Logout", "Are you sure you want to logout?", "Yes", "No");
            if (!confirm) return;

            _ = _loginApi.Logout();
            await _chatHub.StopAsync();
        }


        [RelayCommand]
        async Task Setting()
        {
            var parameters = new Dictionary<string, object>
            {
                //{ "ContactInfo", contact }
            };
            await Shell.Current.GoToAsync(nameof(SettingPage), true, parameters);
        }

    }
}
