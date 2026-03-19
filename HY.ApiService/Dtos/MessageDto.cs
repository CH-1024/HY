using HY.ApiService.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.ApiService.Dtos
{
    public class MessageDto
    {
        public long Id { get; set; }
        public ChatType Chat_Type { get; set; }
        public long Sender_Id { get; set; }
        public string? Sender_Avatar { get; set; }
        public string? Sender_Nickname { get; set; }
        public long Target_Id { get; set; }                 // 对方用户ID / 群ID
        public MessageType Message_Type { get; set; }               // 1文本 2图片 3文件 4语音 5视频
        public string? Content { get; set; }
        public string? Extra { get; set; }
        public MessageStatus Message_Status { get; set; }
        public DateTime Created_At { get; set; }
    }
}
