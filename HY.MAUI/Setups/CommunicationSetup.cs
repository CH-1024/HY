using HY.MAUI.Communication.Auth;
using HY.MAUI.Communication.Http;
using HY.MAUI.Communication.SignalR;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.Setups
{
    public static class CommunicationSetup
    {
        public static void AddCommunicationSetup(this IServiceCollection services)
        {
            // ==============================
            // Services
            // ==============================

            services.AddSingleton<ITokenProvider, TokenProvider>();
            services.AddSingleton<IAuthService, AuthService>();

            services.AddTransient<AuthHttpHandler>();
            services.AddTransient<UnsafeHttpClientHandler>();


            // ==============================
            // AuthService (无 Token)
            // ==============================

            services.AddHttpClient(nameof(AuthService), c =>
            {
                c.Timeout = TimeSpan.FromSeconds(3600);
            })
            .ConfigurePrimaryHttpMessageHandler<UnsafeHttpClientHandler>();


            // ==============================
            // APIs (自动带 Token)
            // ==============================

            AddApiClient<LoginApi>(services, 3600);
            AddApiClient<ChatApi>(services, 3600);
            AddApiClient<MessageApi>(services, 3600);
            AddApiClient<ContactApi>(services, 3600);
            AddApiClient<UserApi>(services, 3600);
            AddApiClient<FileApi>(services, 3600);


            // ==============================
            // SignalR
            // ==============================

            services.AddSingleton<ChatHubSignalR>();
        }


        /// <summary>
        /// 统一注册 API HttpClient
        /// </summary>
        private static void AddApiClient<T>(IServiceCollection services, int timeout) where T : class
        {
            services.AddHttpClient<T>(c =>
            {
                c.Timeout = TimeSpan.FromSeconds(timeout);
            })
            //.ConfigurePrimaryHttpMessageHandler<UnsafeHttpClientHandler>()
            .AddHttpMessageHandler<AuthHttpHandler>();
        }
    }
}
