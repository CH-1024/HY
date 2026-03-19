using HY.ApiService.Services;
using SqlSugar;
using System.Runtime.InteropServices;

namespace HY.ApiService.Setups
{
    public static class RedisSetup
    {
        public static void AddRedisSetup(this IServiceCollection services, IConfiguration configuration)
        {
            // 获取连接字符串
            var conn = configuration.GetConnectionString("Redis");

            services
            .AddStackExchangeRedisCache(options =>
            {
                // Redis 服务器地址
                options.Configuration = conn;

                // 所有缓存键的前缀，用于区分不同应用
                options.InstanceName = "HY:";
            });

            services.AddRedisServices();
        }


        public static void AddRedisServices(this IServiceCollection services)
        {
            // 在这里注册使用 Redis 的服务，例如：
            services.AddSingleton<IRedisTokenService, RedisTokenService>();
        }
    }
}