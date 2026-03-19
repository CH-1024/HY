using HY.MAUI.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.Models
{
    public class ProfileVM
    {
        public long Id { get; set; }
        public string? HYid { get; set; }
        public string? Nickname { get; set; }
        public string? Avatar { get; set; }
        public string? Region { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public UserStatus Status { get; set; }                     // 1正常 0封禁
        public DateTime Created_At { get; set; }
    }
}
