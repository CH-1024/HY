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
        [SugarColumn(IsPrimaryKey = true)]
        public long User_Id { get; set; }
        [SugarColumn(IsPrimaryKey = true)]
        public long Contact_Id { get; set; }
        public long Contact_Request_Id { get; set; }
        public string? Remark { get; set; }
        public RelationStatus Relation_Status { get; set; }                     // 关系状态
        public DateTime Created_At { get; set; }
    }
}
