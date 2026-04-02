using HY.MAUI.Dtos;
using HY.MAUI.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.Mapping
{
    public static class ContactRequestMapping
    {
        public static ContactRequestVM ToVM(this ContactRequestDto dto, long currentUserId)
        {
            var isSelf = dto.Sender_Id == currentUserId;

            return new ContactRequestVM
            {
                Id = dto.Id,
                Sender_Id = dto.Sender_Id,
                Sender_Avatar = dto.Sender_Avatar,
                Sender_Nickname = dto.Sender_Nickname,
                Receiver_Id = dto.Receiver_Id,
                Receiver_Avatar = dto.Receiver_Avatar,
                Receiver_Nickname = dto.Receiver_Nickname,
                Message = dto.Message,
                Source = dto.Source,
                Relation_Request_Status = dto.Relation_Request_Status,
                Created_At = dto.Created_At,
                Handled_At = dto.Handled_At,

                IsSelf = isSelf,
            };
        }

        public static ContactRequestDto ToDto(this ContactRequestVM vm)
        {
            return new ContactRequestDto
            {
                Id = vm.Id,
                Sender_Id = vm.Sender_Id,
                Sender_Avatar = vm.Sender_Avatar,
                Sender_Nickname = vm.Sender_Nickname,
                Receiver_Id = vm.Receiver_Id,
                Receiver_Avatar = vm.Receiver_Avatar,
                Receiver_Nickname = vm.Receiver_Nickname,
                Message = vm.Message,
                Source = vm.Source,
                Relation_Request_Status = vm.Relation_Request_Status,
                Created_At = vm.Created_At,
                Handled_At = vm.Handled_At
            };
        }

    }
}
