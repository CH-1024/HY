using HY.MAUI.Dtos;
using HY.MAUI.Enums;
using HY.MAUI.Models;
using HY.MAUI.Services.Interfaces;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace HY.MAUI.Stores
{
    public class ChatStore
    {
        public ObservableCollection<ChatVM> Chats { get; } = new();

        public ChatVM? GetChat(long chatId)
        {
            return Chats.FirstOrDefault(x => x.Id == chatId);
        }

        public ChatVM? GetChat(long currentUserId, MessageDto messageDto)
        {
            if (currentUserId == messageDto.Sender_Id)
            {
                return Chats.FirstOrDefault(c => c.Target_Id == messageDto.Target_Id && c.Type == messageDto.Chat_Type);
            }
            else if (currentUserId == messageDto.Target_Id)
            {
                return Chats.FirstOrDefault(c => c.Target_Id == messageDto.Sender_Id && c.Type == messageDto.Chat_Type);
            }

            return null;
        }

        public void Upsert(ChatVM chat)
        {
            var existing = Chats.FirstOrDefault(c => c.Id == chat.Id);
            if (existing != null)
            {
                // Update existing
                existing.Id = chat.Id;
                existing.Type = chat.Type;
                existing.Target_Id = chat.Target_Id;
                existing.Target_Name = chat.Target_Name;
                existing.Target_Avatar = chat.Target_Avatar;
                existing.Is_Top = chat.Is_Top;
                existing.Is_Deleted = chat.Is_Deleted;
                existing.Last_Msg_Id = chat.Last_Msg_Id;
                existing.Last_Msg_Type = chat.Last_Msg_Type;
                existing.Last_Msg_Time = chat.Last_Msg_Time;
                existing.Last_Msg_Brief = chat.Last_Msg_Brief;
                existing.Last_Msg_Status = chat.Last_Msg_Status;
                existing.Unread_Count = chat.Unread_Count;
            }
            else
            {
                Chats.Add(chat);
            }
        }

        public void UpsertAndSetTop(ChatVM chat)
        {
            var existing = Chats.FirstOrDefault(c => c.Id == chat.Id);
            if (existing != null)
            {
                // Update existing
                existing.Id = chat.Id;
                existing.Type = chat.Type;
                existing.Target_Id = chat.Target_Id;
                existing.Target_Name = chat.Target_Name;
                existing.Target_Avatar = chat.Target_Avatar;
                existing.Is_Top = chat.Is_Top;
                existing.Is_Deleted = chat.Is_Deleted;
                existing.Last_Msg_Id = chat.Last_Msg_Id;
                existing.Last_Msg_Type = chat.Last_Msg_Type;
                existing.Last_Msg_Time = chat.Last_Msg_Time;
                existing.Last_Msg_Brief = chat.Last_Msg_Brief;
                existing.Last_Msg_Status = chat.Last_Msg_Status;
                existing.Unread_Count = chat.Unread_Count;

                Chats.Move(Chats.IndexOf(existing), 0);
            }
            else
            {
                Chats.Insert(0, chat);
            }
        }

        public bool Remove(ChatType type, long targetId)
        {
            var chat = Chats.FirstOrDefault(c => c.Target_Id == targetId && c.Type == type);
            return Chats.Remove(chat);
        }

        public void Clear()
        {
            Chats.Clear();
        }

    }
}
