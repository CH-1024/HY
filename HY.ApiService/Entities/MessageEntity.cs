using HY.ApiService.Enums;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.ApiService.Entities
{
    [SugarTable("message")]
    public class MessageEntity
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        public ChatType Chat_Type { get; set; }
        public long Sender_Id { get; set; }
        public long Target_Id { get; set; }                     // 对方用户ID / 群ID
        public MessageType Message_Type { get; set; }           // 1文本 2图片 3文件 4语音 5视频
        public string? Content { get; set; }
        public string? Extra { get; set; }
        public MessageStatus Message_Status { get; set; }
        public DateTime Created_At { get; set; }


        /*
{
  "fileId": "uuid-video",
  "duration": 12,
  "w": 1280,
  "h": 720,
  "coverFileId": "uuid-cover"
}         */

    }
}
