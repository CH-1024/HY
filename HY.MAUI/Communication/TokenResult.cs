using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.Communication
{
    public class TokenResult
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime AccessExpiresAt { get; set; }
    }
}
