using HY.ApiService.Enums;

namespace HY.ApiService.Dtos
{
    public class StrangerDto
    {
        public long Stranger_Id { get; set; }
        public string? Nickname { get; set; }
        public string? Avatar { get; set; }
        public UserStatus Status { get; set; }
    }
}
