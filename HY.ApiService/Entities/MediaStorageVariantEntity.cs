using HY.ApiService.Enums;
using SqlSugar;
using StorageType = HY.ApiService.Enums.StorageType;

namespace HY.ApiService.Entities
{
    [SugarTable("media_storage_variant")]
    public class MediaStorageVariantEntity
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        public long Storage_Id { get; set; }
        public VariantType Variant_Type { get; set; }          // 1: 缩略图, 2: 压缩图, 4: 视频封面, 8: 低清视频
        public string File_Path { get; set; } = null!;
        public long File_Size { get; set; }
        public string Mime_Type { get; set; }
        public string Storage_Bucket { get; set; }      // 存储桶
        public StorageType Storage_Type { get; set; }           // 1本地 2OSS 3COS 4S3
        //public int? Width { get; set; }
        //public int? Height { get; set; }
        //public int? Duration { get; set; }                      // 视频/音频时长
        public int Status { get; set; }
        public DateTime Created_At { get; set; }
    }
}
