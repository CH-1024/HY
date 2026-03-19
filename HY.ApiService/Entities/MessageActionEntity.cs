using HY.ApiService.Enums;
using SqlSugar;

namespace HY.ApiService.Entities
{
    [SugarTable("message_user_action")]
    public class MessageActionEntity
    {
        [SugarColumn(IsPrimaryKey = true)]
        public long User_Id { get; set; }
        [SugarColumn(IsPrimaryKey = true)]
        public long Message_Id { get; set; }
        [SugarColumn(IsPrimaryKey = true)]
        public MessageActionType Action_Type { get; set; }
        public DateTime Created_At { get; set; }
    }
}
