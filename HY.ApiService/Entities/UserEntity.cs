using HY.ApiService.Enums;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.ApiService.Entities
{
    [SugarTable("user")]
    public class UserEntity
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        public string? HYid { get; set; }
        public string? Username { get; set; }
        public string? Nickname { get; set; }
        public string? Avatar { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Region { get; set; }
        public string? Password_Hash { get; set; }
        public string? Password_Salt { get; set; }
        public UserStatus Status { get; set; }                     // 1正常 0封禁
        public DateTime Created_At { get; set; }
    }
}
