using System;
using System.Collections.Generic;
using System.Text;

namespace HY.ApiService.Models
{
    public class TokenResult
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime AccessExpiresAt { get; set; }

        public TokenResult()
        {
        }

        public TokenResult(string? accessToken, string? refreshToken, DateTime access_ExpiresAt)
        {
            AccessToken = accessToken;
            RefreshToken = refreshToken;
            AccessExpiresAt = access_ExpiresAt;
        }
    }
}
