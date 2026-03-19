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
                Id = dto.Id,
                Contact_Id = dto.Contact_Id,
                HYid = dto.HYid,
                Nickname = dto.Nickname,
                Avatar = dto.Avatar,
                Region = dto.Region,
                Remark = dto.Remark,
                Status = dto.Status
            };
        }
    }
}
