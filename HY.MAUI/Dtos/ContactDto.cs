using HY.MAUI.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace HY.MAUI.Dtos
{
    public class ContactDto
    {
        public long Id { get; set; }
        public long Contact_Id { get; set; }
        public string? HYid { get; set; }
        public string? Nickname { get; set; }
        public string? Avatar { get; set; }
        public string? Region { get; set; }
        public string? Remark { get; set; }
        public RelationStatus Status { get; set; }                     // 1好友 0拉黑
        public DateTime Created_At { get; set; }
    }
}
