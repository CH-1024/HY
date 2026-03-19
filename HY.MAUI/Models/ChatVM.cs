using CommunityToolkit.Mvvm.ComponentModel;
using HY.MAUI.Enums;
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
        public long Target_Id { get; set; }                                // User  Group
        public string? Target_Name { get; set; }
        public string? Target_Avatar { get; set; }
        public bool Is_Top { get; set; }
        public bool Is_Deleted { get; set; }

        public long Last_Msg_Id { get; set; }
        public MessageType? Last_Msg_Type { get; internal set; }

        private DateTime? last_Msg_Time;
        public DateTime? Last_Msg_Time
        {
            get { return last_Msg_Time; }
            set { SetProperty(ref last_Msg_Time, value); }
        }

        private string? last_Msg_Brief;
        public string? Last_Msg_Brief
        {
            get { return last_Msg_Brief; }
            set { SetProperty(ref last_Msg_Brief, value); }
        }

        private MessageStatus? last_Msg_Status;
        public MessageStatus? Last_Msg_Status
        {
            get { return last_Msg_Status; }
            set { SetProperty(ref last_Msg_Status, value); }
        }

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

        public string Unread_Count_Show => unread_Count > 99 ? "99+" : $"{unread_Count}";
        public bool Unread_Count_Visible => unread_Count > 0;

    }
}
