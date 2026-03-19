using CommunityToolkit.Mvvm.ComponentModel;
using HY.MAUI.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.Models
{
    public partial class StrangerVM : ObservableObject
    {
        public long Stranger_Id { get; set; }
        public string? Nickname { get; set; }
        public string? Avatar { get; set; }
        public UserStatus Status { get; set; }                     // 1正常 0封禁
    }

}
