using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.Dtos
{
    public class GroupDto
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public long Owner_Id { get; set; }
        public string? Avatar { get; set; }
        public DateTime Created_At { get; set; }
    }
}
