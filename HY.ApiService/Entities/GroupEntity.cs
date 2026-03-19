using SqlSugar;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.ApiService.Entities
{
    [SugarTable("group")]
    public class GroupEntity
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        public string? Name { get; set; }
        public long Owner_Id { get; set; }
        public string? Avatar { get; set; }
        public DateTime Created_At { get; set; }
    }
}
