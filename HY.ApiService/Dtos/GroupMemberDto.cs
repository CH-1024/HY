using SqlSugar;

namespace HY.ApiService.Dtos
{
    public class GroupMemberDto
    {
        public long Group_Id { get; set; }
        public long User_Id { get; set; }
        public int Role { get; set; }               // 1群主 2管理员 3普通成员
        public string? Nickname { get; set; }
        public DateTime Created_At { get; set; }
    }
}
