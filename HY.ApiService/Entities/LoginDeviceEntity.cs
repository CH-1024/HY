using SqlSugar;

namespace HY.ApiService.Entities
{
    [SugarTable("login_device")]
    public class LoginDeviceEntity
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        public long User_Id { get; set; }
        public string? Device_Id { get; set; }
        public int Device_Platform { get; set; }
        public int Device_Type { get; set; }
        public string? Device_Name { get; set; }
        public string? Last_Login_Ip { get; set; }
        public DateTime? Last_Login_At { get; set; }
        public bool Is_Online { get; set; }
    }
}
