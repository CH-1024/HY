using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.Communication.Http.Requests
{
    public class UpdateUserRequest
    {
        public string? Nickname { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }
}
