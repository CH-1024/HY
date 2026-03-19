using HY.MAUI.Dtos;
using HY.MAUI.Enums;
using HY.MAUI.Models;
using HY.MAUI.Models.MsgVM;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace HY.MAUI.Mapping
{
    public static class MessageMapping
    {
        public static MessageVM ToVM(this MessageDto dto, long currentUserId)
        {
            var isSelf  = dto.Sender_Id == currentUserId;

            if (dto.Message_Type == MessageType.Text)
            {
                return new TextMessageVM
                {
                    Id = dto.Id,
                    Chat_Type = dto.Chat_Type,
                    Sender_Id = dto.Sender_Id,
                    Sender_Avatar = dto.Sender_Avatar,
                    Sender_Nickname = dto.Sender_Nickname,
                    Target_Id = dto.Target_Id,
                    Message_Status = dto.Message_Status,
                    Created_At = dto.Created_At,
                    IsSelf = isSelf,

                    Content = dto.Content,
                };
            }
            else if (dto.Message_Type == MessageType.Image)
            {
                var extraData = JsonSerializer.Deserialize<Dictionary<string, object?>>(dto.Extra ?? "{}");
                return new ImageMessageVM
                {
                    Id = dto.Id,
                    Chat_Type = dto.Chat_Type,
                    Sender_Id = dto.Sender_Id,
                    Sender_Avatar = dto.Sender_Avatar,
                    Sender_Nickname = dto.Sender_Nickname,
                    Target_Id = dto.Target_Id,
                    Message_Status = dto.Message_Status,
                    Created_At = dto.Created_At,
                    IsSelf = isSelf,
                    File_Id = extraData != null && extraData.TryGetValue("File_Id", out var originalUrl) ? originalUrl?.ToString() : null,
                };
            }
            else if (dto.Message_Type == MessageType.Video)
                throw new NotImplementedException("Voice message mapping not implemented yet.");
            else if (dto.Message_Type == MessageType.System)
                throw new NotImplementedException("System message mapping not implemented yet.");
            else
                throw new NotImplementedException("Message type not implemented in mapping.");
        }

        public static MessageDto ToDto(this MessageVM model)
        {
            if (model is TextMessageVM textMsg)
            {
                return new MessageDto
                {
                    Id = textMsg.Id,
                    Chat_Type = textMsg.Chat_Type,
                    Sender_Id = textMsg.Sender_Id,
                    Sender_Avatar = textMsg.Sender_Avatar,
                    Sender_Nickname = textMsg.Sender_Nickname,
                    Target_Id = textMsg.Target_Id,
                    Message_Type = MessageType.Text,
                    Content = textMsg.Content,
                    Extra = null,
                    Message_Status = textMsg.Message_Status,
                    Created_At = textMsg.Created_At
                };
            }
            else if (model is ImageMessageVM imageMsg)
            {
                return new MessageDto
                {
                    Id = imageMsg.Id,
                    Chat_Type = imageMsg.Chat_Type,
                    Sender_Id = imageMsg.Sender_Id,
                    Sender_Avatar = imageMsg.Sender_Avatar,
                    Sender_Nickname = imageMsg.Sender_Nickname,
                    Target_Id = imageMsg.Target_Id,
                    Message_Type = MessageType.Image,
                    Content = null,
                    Extra = JsonSerializer.Serialize(new Dictionary<string, object?>
                    {
                        { "File_Id", imageMsg.File_Id },
                    }),
                    Message_Status = imageMsg.Message_Status,
                    Created_At = imageMsg.Created_At
                };
            }
            else if (model is VoiceMessageVM voiceMsg)
                throw new NotImplementedException("Voice message mapping not implemented yet.");
            else if (model is SystemMessageVM systemMsg)
                throw new NotImplementedException("System message mapping not implemented yet.");
            else
                throw new NotImplementedException("Message type not implemented in mapping.");
        }


    }
}
