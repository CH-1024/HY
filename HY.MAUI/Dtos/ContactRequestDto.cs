using HY.MAUI.Enums;

namespace HY.MAUI.Dtos
{
    public class ContactRequestDto
    {
        public long Id { get; set; }

        public long Sender_Id { get; set; }
        public string? Sender_Avatar { get; set; }
        public string? Sender_Nickname { get; set; }

        public long Receiver_Id { get; set; }
        public string? Receiver_Avatar { get; set; }
        public string? Receiver_Nickname { get; set; }

        public string? Message { get; set; }                                                        // 验证消息
        public string? Source { get; set; }                                                         // 搜索来源（1 搜索ID  2 手机号  3 群聊  4 二维码  5 名片）
        public RelationRequestStatus Relation_Request_Status { get; set; }                          // 1=待处理 2=已同意 3=已拒绝 4=已撤销 5=已过期
        public DateTime Created_At { get; set; }
        public DateTime Handled_At { get; set; }
    }
}
