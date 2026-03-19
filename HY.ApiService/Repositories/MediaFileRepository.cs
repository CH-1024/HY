using HY.ApiService.Entities;
using SqlSugar;

namespace HY.ApiService.Repositories
{
    public interface IMediaFileRepository
    {
        Task<long> CreateMediaFile(MediaFileEntity mediaFile);

        Task<MediaFileEntity?> GetFileByFileId(string fileId);
    }


    public class MediaFileRepository : IMediaFileRepository
    {

        private readonly IServiceScopeFactory _scopeFactory;

        public MediaFileRepository(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }


        public async Task<long> CreateMediaFile(MediaFileEntity mediaFile)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            mediaFile.Id = await db.Insertable(mediaFile).ExecuteReturnBigIdentityAsync();
            return mediaFile.Id;
        }



        public async Task<MediaFileEntity?> GetFileByFileId(string fileId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            return await db.Queryable<MediaFileEntity>().Where(mf => mf.File_Id == fileId).SingleAsync();
        }

    }
}
