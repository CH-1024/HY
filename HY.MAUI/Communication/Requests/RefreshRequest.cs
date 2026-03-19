namespace HY.MAUI.Communication.Requests
{
    public class RefreshRequest
    {
        public string DeviceId { get; set; } = null!;
        //public string DeviceName { get; set; } = null!;
        public string DevicePlatform { get; set; } = null!;
        //public int DeviceType { get; set; } = 0;

        public string RefreshToken { get; set; } = null!;
    }
}
