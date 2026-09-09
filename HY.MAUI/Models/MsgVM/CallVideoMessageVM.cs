using HY.MAUI.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.Models.MsgVM
{
    public class CallVideoMessageVM : MessageVM
    {
        public string? CallId { get; set; }

        private CallStatus call_Status;
        public CallStatus Call_Status
        {
            get { return call_Status; }
            set { SetProperty(ref call_Status, value); }
        }

        public TimeSpan Duration { get; set; }
    }
}
