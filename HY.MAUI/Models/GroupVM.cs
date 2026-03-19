using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.Models
{
    public partial class GroupVM : ObservableObject
    {
        public long Id { get; set; }
        public long Owner_Id { get; set; }
        public string? Group_Name { get; set; }
        public string? Group_Avatar { get; set; }
        public string? Group_Notice { get; set; }
        //public int Member_Count { get; internal set; }
        //public DateTime Created_At { get; set; }
    }
}
