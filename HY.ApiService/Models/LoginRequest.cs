using System;
using System.Collections.Generic;
using System.Text;

namespace HY.ApiService.Models
{
    public class LoginRequest
    {
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string DeviceId { get; set; } = null!;
        public string DeviceName { get; set; } = null!;
        public int DevicePlatform { get; set; } = -1;
        public int DeviceType { get; set; } = 0;
    }
}
