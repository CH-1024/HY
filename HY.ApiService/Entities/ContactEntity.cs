using HY.ApiService.Enums;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace HY.ApiService.Entities
{
    [SugarTable("contact")]
    public class ContactEntity
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        public long User_Id { get; set; }
        public long Contact_Id { get; set; }
        public string? Remark { get; set; }
        public RelationStatus Status { get; set; }                     // 1好友 0拉黑
        public DateTime Created_At { get; set; }
    }
}
