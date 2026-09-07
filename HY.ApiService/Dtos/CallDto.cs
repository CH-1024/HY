using HY.ApiService.Enums;
using System.Runtime.CompilerServices;

namespace HY.ApiService.Dtos
{
    public class CallDto
    {        
        public string CallId { get; set; }

        public CallType CallType { get; set; }
        public ChatType ChatType { get; set; }

        public long CallerId { get; set; }
        public int CallerPlatform { get; set; }

        public long CalleeId { get; set; }
        public int CalleePlatform { get; set; }

        public CallState CallState { get; set; }

        public DateTime CreateAt { get; set; }
        public DateTime ExpiryAt { get; set; }
    }
}
