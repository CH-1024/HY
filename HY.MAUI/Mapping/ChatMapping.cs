using HY.MAUI.Dtos;
using HY.MAUI.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.Mapping
{
    public static class ChatMapping
    {
        public static ChatVM ToVM(this ChatDto dto)
        {
            return new ChatVM
            {
                Id = dto.Id,
                Type = dto.Type,
                Target_Id = dto.Target_Id,
                Target_Name = dto.Target_Name,
                Target_Avatar = dto.Target_Avatar,
                Is_Top = dto.Is_Top,
                Is_Deleted = dto.Is_Deleted,
                Last_Msg_Id = dto.Last_Msg_Id,
                Last_Msg_Type = dto.Last_Msg_Type,
                Last_Msg_Time = dto.Last_Msg_Time,
                Last_Msg_Brief = dto.Last_Msg_Brief,
                Last_Msg_Status = dto.Last_Msg_Status,
                Unread_Count = dto.Unread_Count,
            };
        }

    }
}
