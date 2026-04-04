using HY.ApiService.Entities;
using HY.ApiService.Enums;
using SqlSugar;
using StorageType = HY.ApiService.Enums.StorageType;

namespace HY.ApiService.Repositories
{
    public interface IMediaStorageVariantRepository
    {
        Task<long> CreateMediaStorageVariant(MediaStorageVariantEntity mediaStorageVariant);

        Task<MediaStorageVariantEntity> GetMediaStorageVariantByStorageIdAndVariantType(long storage_Id, VariantType thumbnail);

        Task<bool> UpdateMediaStorageVariant(long storageId, VariantType variantType, string filePath, string mimeType, string bucket, StorageType storageType, int status);
        Task<bool> UpdateMediaStorageVariant(MediaStorageVariantEntity mediaStorageVariant);

    }


    public class MediaStorageVariantRepository : IMediaStorageVariantRepository
    {
        private readonly ISqlSugarClient _db;

        public MediaStorageVariantRepository(ISqlSugarClient db)
        {
            _db = db;
        }


        public async Task<long> CreateMediaStorageVariant(MediaStorageVariantEntity mediaStorageVariant)
        {
            mediaStorageVariant.Id = await _db.Insertable(mediaStorageVariant).ExecuteReturnBigIdentityAsync();
            return mediaStorageVariant.Id;
        }




        public async Task<MediaStorageVariantEntity> GetMediaStorageVariantByStorageIdAndVariantType(long storageId, VariantType variantType)
        {
            return await _db.Queryable<MediaStorageVariantEntity>().Where(msv => msv.Storage_Id == storageId && msv.Variant_Type == variantType).SingleAsync();
        }




        public async Task<bool> UpdateMediaStorageVariant(long storageId, VariantType variantType, string filePath, string mimeType, string bucket, StorageType storageType, int status)
        {
            return await _db.Updateable<MediaStorageVariantEntity>()
                .SetColumns(msv => msv.File_Path == filePath)
                .SetColumns(msv => msv.Mime_Type == mimeType)
                .SetColumns(msv => msv.Storage_Bucket == bucket)
                .SetColumns(msv => msv.Storage_Type == storageType)
                .SetColumns(msv => msv.Status == status)
                .Where(msv => msv.Storage_Id == storageId && msv.Variant_Type == variantType)
                .ExecuteCommandAsync() > 0;
        }

        public async Task<bool> UpdateMediaStorageVariant(MediaStorageVariantEntity mediaStorageVariant)
        {
            var result = await _db.Updateable(mediaStorageVariant).ExecuteCommandAsync();
            return result > 0;
        }

    }
}
