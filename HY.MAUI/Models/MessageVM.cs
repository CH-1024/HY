using CommunityToolkit.Mvvm.ComponentModel;
using HY.MAUI.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using HY.MAUI.Models.MsgVM;

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

        
        
        // 扩展
        public DateTime Msg_Created_LocalTime => DateTime.SpecifyKind(this.Created_At, DateTimeKind.Utc).ToLocalTime();

        public MessageType? Msg_Type => this switch
        {
            TextMessageVM => MessageType.Text,
            ImageMessageVM => MessageType.Image,
            FileMessageVM => MessageType.File,
            VoiceMessageVM => MessageType.Voice,
            VideoMessageVM => MessageType.Video,
            SystemMessageVM => MessageType.System,
            CallVoiceMessageVM => MessageType.VoiceCall,
            CallVideoMessageVM => MessageType.VideoCall,
            _ => null
        };
        
        public string? Msg_Brief => this switch
        {
            TextMessageVM textVM => textVM.Content?.Length > 20 ? textVM.Content.Replace("\r", "").Replace("\n", "").Substring(0, 20) + "..." : textVM.Content?.Replace("\r", "").Replace("\n", ""),
            ImageMessageVM => "[图片]",
            FileMessageVM => "[文件]",
            VoiceMessageVM => "[语音]",
            VideoMessageVM => "[视频]",
            SystemMessageVM sysVM=> sysVM.Text?.Length > 20 ? sysVM.Text.Substring(0, 20) + "..." : sysVM.Text,
            CallVoiceMessageVM => "[语音通话]",
            CallVideoMessageVM => "[视频通话]",
            _ => null
        };
    }
}
