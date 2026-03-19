namespace HY.ApiService.Setups
{
    public static class ControllersSetup
    {
        public static void AddControllersSetup(this IServiceCollection services)
        {
            services
            .AddControllers(options =>
            {

            })
            .AddJsonOptions(options =>
            {
                // 配置 JSON 序列化选项，例如忽略循环引用
                //options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.PropertyNamingPolicy = null; // 保留原有的大小写
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true; 
            });
        }
    }
}
