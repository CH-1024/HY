using CommunityToolkit.Mvvm.ComponentModel;
using HY.MAUI.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.Models
{
    public abstract class MessageVM : ObservableObject
    {
        public long Id { get; set; }
        public ChatType Chat_Type { get; set; }
        public long Sender_Id { get; set; }
        public string? Sender_Avatar { get; set; }
        public string? Sender_Nickname { get; set; }
        public long Target_Id { get; set; }                 // 对方用户ID / 群ID
        public DateTime Created_At { get; set; }
        public bool IsSelf { get; set; }

        private MessageStatus message_Status;
        public MessageStatus Message_Status
        {
            get { return message_Status; }
            set { SetProperty(ref message_Status, value); }
        }

        //public string? Type { get; set; }                   // 1文本 2图片 3文件 4语音 5视频
        //public string? Content { get; set; }
        //public string? Extra { get; set; }
        //public int Avatar_InColumn { get; set; }
        //public LayoutOptions Content_Horizontal { get; set; }
    }
}
