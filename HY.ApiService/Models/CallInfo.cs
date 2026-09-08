using HY.ApiService.Enums;
using System.Runtime.CompilerServices;

namespace HY.ApiService.Models
{
    public class CallInfo
    {        
        public string CallId { get; set; }

        public CallType CallType { get; set; }
        public ChatType ChatType { get; set; }

        public long CallerId { get; set; }
        public int CallerPlatform { get; set; }

        public long CalleeId { get; set; }
        public int CalleePlatform { get; set; }

        public CallStatus CallState { get; set; }

        public DateTime CreateAt { get; set; }
        public DateTime ExpiryAt { get; set; }
    }
}
