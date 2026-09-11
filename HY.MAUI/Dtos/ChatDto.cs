using HY.MAUI.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace HY.MAUI.Dtos
{
    public class ChatDto
    {
        public long Id { get; set; }
        public ChatType Type { get; set; }                                      // 1单聊 2群聊
        public long Target_Id { get; set; }
        public string? Target_Name { get; set; }
        public string? Target_Avatar { get; set; }
        public bool Is_Top { get; set; }
        public bool Is_Deleted { get; set; }
        public int Unread_Count { get; set; }

        public MessageDto? Last_Msg { get; set; }
    }
}
