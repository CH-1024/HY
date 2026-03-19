using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.Enums
{
    public enum UserStatus
    {
        Registered,    // 已注册	    已完成基础注册，但未验证或信息不全	    邮箱/手机注册后，等待验证
        Active,        // 活跃          核心正常状态，可正常使用所有功能	    完成验证、信息完善的用户
        Inactive,      // 未活跃        系统判定长期未登录或未操作	            用于用户召回策略
        Suspended,     // 已停用        管理员手动操作，临时限制权限	        违反社区规则，暂时封禁
        Banned,        // 已封禁        管理员手动操作，永久或长期封禁	        严重违规，账号清零
        Deactivated,   // 已停用        用户主动操作，暂时关闭账号	            用户暂时不想使用，可恢复
        Deleted        // 已注销        用户主动或系统触发，逻辑删除	        符合GDPR等合规要求，数据保留期
    }
}
