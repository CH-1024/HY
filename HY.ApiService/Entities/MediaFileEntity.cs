using HY.ApiService.Enums;
using SqlSugar;

namespace HY.ApiService.Entities
{
    [SugarTable("media_file")]
    public class MediaFileEntity
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        public string File_Id { get; set; } = null!;            // 文件ID（GUID）
        public long Storage_Id { get; set; }
        public long User_Id { get; set; }
        public FileType File_Type { get; set; }                      // 1: 图片, 2: 视频, 3: 音频, 4: 文件
        public string? Original_Name { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public int? Duration { get; set; }                      // 视频或音频时长（秒）
        public int Status { get; set; }
        public DateTime Created_At { get; set; }
    }
}
