using SqlSugar;

namespace HY.ApiService.Entities
{
    [SugarTable("contact_request")]
    public class ContactRequestEntity
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        public long Sender_Id { get; set; }
        public long Receiver_Id { get; set; }
        public string? Message { get; set; }                     // 验证消息
        public string? Source { get; set; }                      // 搜索来源（1 搜索ID  2 手机号  3 群聊  4 二维码  5 名片）
        public int Status { get; set; }                          // 0=待处理 1=已同意 2=已拒绝 3=已撤销 4=已过期
        public DateTime Created_At { get; set; }
        public DateTime Handled_At { get; set; }

    }
}
