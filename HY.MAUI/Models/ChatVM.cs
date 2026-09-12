using CommunityToolkit.Mvvm.ComponentModel;
using HY.MAUI.Enums;
using HY.MAUI.Models.MsgVM;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.Json.Serialization;

namespace HY.MAUI.Models
{
    public partial class ChatVM : ObservableObject
    {
        public long Id { get; set; }
        public ChatType Type { get; set; }                                      // 1单聊 2群聊
        public long Target_Id { get; set; }                                     // User  Group
        public string? Target_Name { get; set; }
        public string? Target_Avatar { get; set; }
        public bool Is_Top { get; set; }
        public bool Is_Deleted { get; set; }

        private int unread_Count;
        public int Unread_Count
        {
            get { return unread_Count; }
            set
            {
                unread_Count = value;
                OnPropertyChanged(nameof(Unread_Count_Show));
                OnPropertyChanged(nameof(Unread_Count_Visible));
            }
        }

        private MessageVM? last_Msg;
        public MessageVM? Last_Msg
        {
            get { return last_Msg; }
            set
            {
                SetProperty(ref last_Msg, value);
                OnPropertyChanged(nameof(Last_Msg_Type));
                OnPropertyChanged(nameof(Last_Msg_Time));
                OnPropertyChanged(nameof(Last_Msg_Brief));
            }
        }







        public string Unread_Count_Show => unread_Count > 99 ? "99+" : $"{unread_Count}";
        public bool Unread_Count_Visible => unread_Count > 0;

        public MessageType? Last_Msg_Type => Last_Msg switch
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

        public DateTime? Last_Msg_Time => Last_Msg != null ? DateTime.SpecifyKind(Last_Msg.Created_At, DateTimeKind.Utc).ToLocalTime() : null;

        public string? Last_Msg_Brief => Last_Msg switch
        {
            TextMessageVM textVM => textVM.Content?.Length > 20 ? textVM.Content.Substring(0, 20) + "..." : textVM.Content,
            ImageMessageVM => "[图片]",
            FileMessageVM => "[文件]",
            VoiceMessageVM => "[语音]",
            VideoMessageVM => "[视频]",
            SystemMessageVM sysVM=> sysVM.Text?.Length > 20 ? sysVM.Text.Substring(0, 20) + "..." : sysVM.Text,
            CallVoiceMessageVM => "[语音通话]",
            CallVideoMessageVM => "[视频通话]",
            _ => null
        };





        // 扩展
        public bool IsMsgEnd { get; set; } = false;
    }
}
