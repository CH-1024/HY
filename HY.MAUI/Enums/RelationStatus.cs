using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.Enums
{
    public enum RelationStatus
    {
        None,       // 无任何关系	                            A和B无关
        Pending,    // 请求已发送，等待对方处理	                对发送方显示“等待验证”；对接收方显示“收到请求”
        Accepted,   // 请求已被接受，关系正式建立	            双方均显示为“好友/已连接”
        Declined,   // 请求被明确拒绝	                        通常对发送方显示“已拒绝”，接收方可能不显示此记录
        Blocked,    // 单向阻断。A拉黑B，不影响B的状态视图  	对A：B在黑名单。对B：A可能显示为none或deleted
        Unfriended  // 解除已建立的关系	                        关系解除后，可回到none状态，或保留此状态用于记录。
    }
}
