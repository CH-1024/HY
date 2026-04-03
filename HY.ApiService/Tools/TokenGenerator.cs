using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace HY.ApiService.Tools
{
    public static class TokenGenerator
    {
        public static string GenerateAccessToken(long userId, string deviceId, string devicePlatform, out DateTime expires)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("your-secret-key-at-least-16-chars"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim("DeviceId", deviceId),
                new Claim("DevicePlatform", devicePlatform),
                //new Claim(ClaimTypes.Name, user.Username),
                //new Claim(ClaimTypes.Role, "Admin"),
                //new Claim("Age", "25")
            };

            expires = DateTime.UtcNow.AddMinutes(30);

            var token = new JwtSecurityToken(
                issuer: "your_issuer",
                audience: "your_audience",
                claims: claims,
                expires: expires,
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public static string GenerateRefreshToken()
        {
            var bytes = new byte[32]; // 256 bit
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToHexString(bytes).ToLower(); // 64 chars
        }

    }
}
