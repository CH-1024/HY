using CommunityToolkit.Mvvm.ComponentModel;
using HY.MAUI.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace HY.MAUI.Models
{
    public partial class UserVM : ObservableObject
    {
        public long Id { get; set; }
        public string? HYid { get; set; }
        public string? Nickname { get; set; }

        private string? avatar;
        public string? Avatar
        {
            get { return avatar; }
            set { SetProperty(ref avatar, value); }
        }

        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Region { get; set; }
        public UserStatus Status { get; set; }                     // 1正常 0封禁
        public DateTime Created_At { get; set; }
    }
}
