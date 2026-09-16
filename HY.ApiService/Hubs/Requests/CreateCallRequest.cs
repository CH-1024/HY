using HY.ApiService.Enums;

namespace HY.ApiService.Hubs.Requests
{
    public class CreateCallRequest
    {
        public CallType CallType { get; set; }
        public long CalleeId { get; set; }
    }
}
