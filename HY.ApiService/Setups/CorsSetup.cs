namespace HY.ApiService.Setups
{
    /// <summary>
    /// CORS 配置
    /// </summary>
    public static class CorsSetup
    {
        public static void AddCorsSetup(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                // 允许所有
                options.AddPolicy("AllowAll",
                    policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

                // 允许特定域
                options.AddPolicy("AllowMySite",
                    policy => policy.WithOrigins("https://example.com", "http://localhost:3000")
                                    .AllowAnyMethod()
                                    .AllowAnyHeader());

                // 更严格的配置
                options.AddPolicy("StrictPolicy",
                    policy => policy
                        .WithOrigins("https://trusted.com")
                        .WithMethods("GET", "POST")
                        .WithHeaders("Content-Type", "Authorization")
                        .AllowCredentials());  // 允许发送 cookies

            });
        }
    }
}
