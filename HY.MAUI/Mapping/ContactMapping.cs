using HY.MAUI.Dtos;
using HY.MAUI.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.Mapping
{
    public static class ContactMapping
    {
        public static ContactVM ToVM(this ContactDto dto)
        {
            return new ContactVM
            {
                Contact_Id = dto.Contact_Id,
                HYid = dto.HYid,
                Nickname = dto.Nickname,
                Avatar = dto.Avatar,
                Region = dto.Region,
                Remark = dto.Remark,
                Contact_Status = dto.Contact_Status,
                Relation_Request_Status = dto.Relation_Request_Status,
                Relation_Status = dto.Relation_Status
            };
        }
    }
}
