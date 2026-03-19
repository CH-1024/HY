using HY.MAUI.Dtos;
using HY.MAUI.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.Mapping
{
    public static class StrangerMapping
    {
        public static StrangerVM ToVM(this StrangerDto dto)
        {
            return new StrangerVM
            {
                Stranger_Id = dto.Stranger_Id,
                Nickname = dto.Nickname,
                Avatar = dto.Avatar,
                Status = dto.Status
            };
        }

    }
}
