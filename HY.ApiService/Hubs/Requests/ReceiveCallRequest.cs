using HY.ApiService.Enums;

namespace HY.ApiService.Hubs.Requests
{
    public class ReceiveCallRequest
    {
        public string CallId { get; set; }
        public CallType CallType { get; set; }
        public ChatType ChatType { get; set; }
        public long CallerId { get; set; }
    }
}
