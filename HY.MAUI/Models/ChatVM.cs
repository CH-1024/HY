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
        public MessageVM? Last_Msg      // 注意 ：从 ChatVM 拿到的 Last_Msg 会丢失 IsSelf、Sender_Avatar、Sender_Nickname 属性
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




        


        // 扩展
        public bool IsMsgEnd { get; set; } = false;
        
        public string Unread_Count_Show => unread_Count > 99 ? "99+" : $"{unread_Count}";
        
        public bool Unread_Count_Visible => unread_Count > 0;
        
        public DateTime? Last_Msg_Time => Last_Msg?.Msg_Created_LocalTime;

        public MessageType? Last_Msg_Type => Last_Msg?.Msg_Type;

        public string? Last_Msg_Brief => Last_Msg?.Msg_Brief;

    }
}
