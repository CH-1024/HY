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
            var old = Chats.FirstOrDefault(x => x.Id == chat.Id);
            if (old == null) Chats.Add(chat);
            else Chats.Insert(Chats.IndexOf(old), chat);
        }

        public void Clear()
        {
            Chats.Clear();
        }

    }
}
