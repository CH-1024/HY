using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HY.MAUI.Models;
using HY.MAUI.Models.MsgVM;
using HY.MAUI.Pages.Chat;
using HY.MAUI.Pages.Shells;
using HY.MAUI.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI
{
    public partial class AppShellModel : ObservableObject
    {
        ILoginService _loginService;


        private UserVM? currentUser;
        public UserVM? CurrentUser
        {
            get { return currentUser; }
            set { SetProperty(ref currentUser, value); }
        }

        public AppShellModel(ILoginService loginService)
        {
            _loginService = loginService;
        }


        [RelayCommand]
        async Task Appearing()
        {
            CurrentUser = await _loginService.GetCurrentUser();
        }


        [RelayCommand]
        async Task ShowPage(string rote)
        {
            await Shell.Current.GoToAsync(rote, false);
        }


    }
}
