using CommunityToolkit.Mvvm.ComponentModel;
using HY.MAUI.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace HY.MAUI.Models
{
    public partial class ContactVM : ObservableObject
    {
        public long Contact_Id { get; set; }
        public string? HYid { get; set; }
        public string? Nickname { get; set; }
        public string? Avatar { get; set; }
        public string? Region { get; set; }
        public string? Remark { get; set; }
        public UserStatus Contact_Status { get; set; }                          // 联系人状态
        public RelationStatus Relation_Status { get; set; }                     // 关系状态
        //public DateTime Created_At { get; set; }
    }
}
