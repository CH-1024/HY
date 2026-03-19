using HY.MAUI.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.PageModels.Chat.MessageCommands
{
    public class MessageCommandInvocation
    {
        public string? Command { get; set; }
        public MessageVM? Message { get; set; }
        public Dictionary<string, object>? Parameters { get; set; }
    }
}
