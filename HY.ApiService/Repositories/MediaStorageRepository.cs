using HY.ApiService.Entities;
using SqlSugar;

namespace HY.ApiService.Repositories
{
    public interface IMediaStorageRepository
    {
        Task<long> CreateMediaStorage(MediaStorageEntity mediaStorage);

        Task<MediaStorageEntity?> GetMediaStorageById(long Id);
        Task<MediaStorageEntity?> GetMediaStorageByMD5(string md5);

        Task<bool> UpdateMediaStorage(MediaStorageEntity mediaStorageEntity);
        Task<bool> UpdateMediaStorageRefCount(long id, int ref_Count);
    }


    public class MediaStorageRepository : IMediaStorageRepository
    {

        private readonly IServiceScopeFactory _scopeFactory;

        public MediaStorageRepository(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }


        public async Task<long> CreateMediaStorage(MediaStorageEntity mediaStorage)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            mediaStorage.Id = await db.Insertable(mediaStorage).ExecuteReturnBigIdentityAsync();
            return mediaStorage.Id;
        }





        public async Task<MediaStorageEntity?> GetMediaStorageById(long Id)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            return await db.Queryable<MediaStorageEntity>().Where(ms => ms.Id == Id && ms.Status == 1).SingleAsync();
        }

        public async Task<MediaStorageEntity?> GetMediaStorageByMD5(string md5)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            return await db.Queryable<MediaStorageEntity>().Where(ms => ms.File_MD5 == md5).SingleAsync();
        }




        public async Task<bool> UpdateMediaStorage(MediaStorageEntity mediaStorageEntity)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var result = await db.Updateable(mediaStorageEntity).ExecuteCommandAsync();
            return result > 0;
        }

        public async Task<bool> UpdateMediaStorageRefCount(long id, int ref_Count)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var result = await db.Updateable<MediaStorageEntity>()
                .SetColumns(ms => ms.Ref_Count == ref_Count)
                .Where(ms => ms.Id == id)
                .ExecuteCommandAsync();
            return result > 0;
        }

    }
}
