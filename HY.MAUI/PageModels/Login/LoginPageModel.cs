using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HY.MAUI.Communication;
using HY.MAUI.Communication.Auth;
using HY.MAUI.Communication.Http;
using HY.MAUI.Communication.SignalR;
using HY.MAUI.Dtos;
using HY.MAUI.Mapping;
using HY.MAUI.Pages.Login;
using HY.MAUI.Services;
using HY.MAUI.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace HY.MAUI.PageModels.Login
{
    public partial class LoginPageModel : ObservableObject
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILoginService _loginService;
        private readonly ITokenProvider _tokenProvider;
        private readonly LoginApi _loginApi;

        private string userName;
        public string Username
        {
            get { return userName; }
            set { SetProperty(ref userName, value); }
        }

        private string password;
        public string Password
        {
            get { return password; }
            set { SetProperty(ref password, value); }
        }


        public LoginPageModel(IServiceProvider serviceProvider, ILoginService loginService, ITokenProvider tokenProvider, LoginApi loginApi)
        {
            _serviceProvider = serviceProvider;
            _loginService = loginService;
            _tokenProvider = tokenProvider;
            _loginApi = loginApi;
        }

        [RelayCommand]
        void Appearing()
        {
            _loginService.Logout();
            _tokenProvider.Clear();
        }

        [RelayCommand]
        async Task Login()
        {
            //var p1 = await _loginApi.Ping1();
            //var p2 = await _loginApi.Ping2();

            var loginResp = await _loginApi.Login(Username, Password);
            if (loginResp?.IsSucc == true)
            {
                var tokens = loginResp.GetValue<TokenResult>("Tokens");
                await _tokenProvider.SetAsync(tokens!);

                var userDto = loginResp.GetValue<UserDto>("User")!;
                await _loginService.Login(userDto.ToVM());

                var loadingPage = _serviceProvider.GetRequiredService<LoadingPage>();
                Application.Current!.Windows[0].Page = new NavigationPage(loadingPage);
            }
            else
            {
                _ = Application.Current!.Windows[0].Page!.DisplayAlertAsync("失败", loginResp?.Msg ?? "Unknown error", "OK");
            }
        }

        [RelayCommand]
        async Task Register()
        {
            var registerPage = _serviceProvider.GetRequiredService<RegisterPage>();
            await Application.Current!.Windows[0].Page!.Navigation.PushAsync(registerPage, true);
        }

    }
}
