namespace HY.ApiService.Setups
{
    public static class SignalRSetup
    {
        public static void AddSignalRSetup(this IServiceCollection services)
        {
            services
            .AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
                options.MaximumReceiveMessageSize = 102400000; // 100 MB
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(30); 
            })
            .AddJsonProtocol(options =>
             {
                 options.PayloadSerializerOptions.PropertyNamingPolicy = null; // 保留原有的大小写
                 //options.PayloadSerializerOptions.Converters.Clear(); // 移除默认转换器
             });
        }
    }
}
