using SqlSugar;

namespace HY.ApiService.Entities
{
    [SugarTable("login_token")]
    public class LoginTokenEntity
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        public long User_Id { get; set; }
        public string? Device_Id { get; set; }
        public string? Refresh_Token { get; set; }
        public DateTime Refresh_Expired { get; set; }
        public bool Revoked { get; set; }
        public DateTime Created_At { get; set; }
    }
}
