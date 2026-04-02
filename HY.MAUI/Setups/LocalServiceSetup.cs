using HY.MAUI.Services;
using HY.MAUI.Services.Interfaces;
using HY.MAUI.Stores;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.Setups
{
    public static class LocalServiceSetup
    {
        public static void AddLocalServiceSetup(this IServiceCollection services)
        {
            services.AddSingleton<IGlobalCache, GlobalCache>();
            services.AddSingleton<ILoginService, LoginService>();

            services.AddSingleton<ChatStore>();
            services.AddSingleton<ContactStore>();
            services.AddSingleton<ContactRequestStore>();
            services.AddSingleton<MessageStore>();
        }
    }
}
