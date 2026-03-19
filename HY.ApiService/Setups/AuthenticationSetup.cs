using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using HY.ApiService.Services;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace HY.ApiService.Setups
{
    /// <summary>
    /// 身份验证配置
    /// </summary>
    public static class AuthenticationSetup
    {
        public static void AddAuthenticationSetup(this IServiceCollection services)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = "your_issuer",
                    ValidAudience = "your_audience",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("your-secret-key-at-least-16-chars"))
                };

                // JWT 配置
                // ⭐ SignalR 必须加
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chatHub"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async context =>
                    {
                        // 在鉴权之前，检查 token 是否在 Redis 中存在
                        var tokenStore = context.HttpContext.RequestServices.GetService<IRedisTokenService>();
                        if (tokenStore == null) return;

                        var userIdClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        var deviceId = context.Principal?.FindFirst("DeviceId")?.Value;
                        var token = context.SecurityToken as JwtSecurityToken;
                        var rawToken = token != null ? new JwtSecurityTokenHandler().WriteToken(token) : context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

                        if (!long.TryParse(userIdClaim, out var userId) || string.IsNullOrEmpty(deviceId) || string.IsNullOrEmpty(rawToken))
                        {
                            context.Fail("Invalid token claims");
                            return;
                        }

                        var exists = await tokenStore.ExistsAsync(userId, deviceId, rawToken);
                        if (!exists)
                        {
                            context.Fail("Token not found or revoked");
                        }
                    }
                };


            });

        }
    }
}
