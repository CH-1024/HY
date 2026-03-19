using HY.ApiService.Enums;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace HY.ApiService.Entities
{
    [SugarTable("chat")]
    public class ChatEntity
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        public ChatType Type { get; set; }                                      // 1单聊 2群聊
        public long User_Id { get; set; }
        public long Target_Id { get; set; }
        public long Last_Msg_Id { get; set; }
        public long Read_Msg_Id { get; set; }
        public int Unread_Count { get; set; }
        public bool Is_Top { get; set; }
        public bool Is_Deleted { get; set; }
        public DateTime? Last_Msg_Time { get; set; }

    }
}
