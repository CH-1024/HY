namespace HY.MAUI.Enums
{
    public enum RelationRequestStatus
    {
        None,       // 无任何请求记录
        Pending,    // 请求已发送，等待对方处理	                对发送方显示“等待验证”；对接收方显示“收到请求”
        Accepted,   // 请求已被接受，关系正式建立	            双方均显示为“好友/已连接”
        Declined,   // 请求被明确拒绝	                        通常对发送方显示“已拒绝”，接收方可能不显示此记录
        Revoked,    // 请求被撤销	                            发送方撤销请求后，接收方可能不显示此记录
        Expired     // 请求已过期	                            超过一定时间未处理，系统自动将请求标记为过期
    }
}
