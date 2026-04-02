using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.Enums
{
    public enum MessageStatus
    {
        Sending = 0,    // 本地已发，未确认（数据库禁用，仅限DTO）
        Sented = 1,       // 服务器确认
        Failed = 2,     // 发送失败，未确认（数据库禁用，仅限DTO）

        Recalling = 3,  // 本地已撤回，未确认（数据库禁用，仅限DTO）
        Recalled = 4,     // 撤回

        Deleting = 5,   // 本地已删除，未确认（数据库禁用，仅限DTO）
        Deleted = 6       // 本地已删除，确认（数据库禁用，仅限DTO）
    }
}
