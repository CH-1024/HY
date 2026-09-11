using HY.ApiService.Services;

namespace HY.ApiService.Setups
{
    public static class SignalRSetup
    {
        public static void AddSignalRSetup(this IServiceCollection services, IConfiguration configuration)
        {
            // 获取连接字符串
            var keepAlive = configuration.GetSection("SignalR")?.GetValue<long>("KeepAlive") ?? 15;
            var clientTimeout = configuration.GetSection("SignalR")?.GetValue<long>("ClientTimeout") ?? 30;

            services
            .AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
                options.MaximumReceiveMessageSize = 102400000; // 100 MB
                options.KeepAliveInterval = TimeSpan.FromSeconds(keepAlive);
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(clientTimeout);
            })
            .AddJsonProtocol(options =>
             {
                 options.PayloadSerializerOptions.PropertyNamingPolicy = null; // 保留原有的大小写
                 //options.PayloadSerializerOptions.Converters.Clear(); // 移除默认转换器
             });

            services.AddSignalRServices();
        }

        public static void AddSignalRServices(this IServiceCollection services)
        {
            // 在这里注册使用 SignalR 的服务，例如：
            services.AddScoped<IChatNotificationService, ChatNotificationService>();
        }
    }
}
