namespace HY.ApiService.Enums
{
    public enum MessageStatus
    {
        //Sending = 0,    // 本地已发，未确认（客户端专用）
        Sented = 1,       // 服务器确认
        //Failed = 2,     // 发送失败，未确认（客户端专用）

        //Recalling = 3,  // 本地已撤回，未确认（客户端专用）
        Recalled = 4,     // 撤回

        //Deleting = 5,   // 本地已删除，未确认（客户端专用）
        Deleted = 6       // 本地已删除，确认（服务器禁用 客户端专用）
    }
}
