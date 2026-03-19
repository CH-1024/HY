using Microsoft.AspNetCore.Authorization;

namespace HY.ApiService.Setups
{
    public static class AuthorizationSetup
    {
        /// <summary>
        /// 添加授权配置
        /// </summary>
        public static void AddAuthorizationSetup(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));

                options.AddPolicy("Over18", policy => policy.RequireClaim("Age", "18", "19"));

                //options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build(); // 全局要求认证

            });
        }
    }
}
