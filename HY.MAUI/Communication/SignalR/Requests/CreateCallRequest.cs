using HY.MAUI.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.Communication.SignalR.Requests
{
    public class CreateCallRequest
    {
        public CallType CallType { get; set; }
        public ChatType ChatType { get; set; }
        public long CalleeId { get; set; }
    }
}
