namespace HY.ApiService.Enums
{
    public enum RelationStatus
    {
        None = 0,           // 无任何关系（数据库禁用，仅限DTO）
        Friend = 1,         // 正常好友
        Deleted = 2,        // 已删除
        Blocked = 3,        // 拉黑对方
    }
}
