using HY.MAUI.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.Dtos
{
    public class StrangerDto
    {
        public long Stranger_Id { get; set; }
        public string? Nickname { get; set; }
        public string? Avatar { get; set; }
        public UserStatus Status { get; set; }
    }
}
