using HY.MAUI.Communication.SendQueue;
using HY.MAUI.Pages.Login;
using HY.MAUI.Services;
using HY.MAUI.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace HY.MAUI
{
    public partial class App : Application
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILoginService _loginService;

        public App(IServiceProvider serviceProvider, ILoginService loginService, MessageSendWorker worker)
        {
            InitializeComponent();

            _serviceProvider = serviceProvider;
            _loginService = loginService;

            _ = worker.RunAsync(CancellationToken.None);
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // 切换到深色模式
            Application.Current!.UserAppTheme = AppTheme.Dark;

            var window = new Window();

#if WINDOWS || MACCATALYST
            window.MinimumHeight = 900;
            window.MaximumHeight = 900;
            window.MinimumWidth = 600;
            window.MaximumWidth = 600;

            window.Height = 900;
            window.Width = 600;
#endif

            if (_loginService.IsLoggedIn)
            {
                var loadingPage = _serviceProvider.GetRequiredService<LoadingPage>();
                window.Page = new NavigationPage(loadingPage);
            }
            else
            {
                var loginPage = _serviceProvider.GetRequiredService<LoginPage>();
                window.Page = new NavigationPage(loginPage);
            }

            return window;
        }
    }
}