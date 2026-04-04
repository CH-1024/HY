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
        private readonly ISqlSugarClient _db;

        public MediaFileRepository(ISqlSugarClient db)
        {
            _db = db;
        }


        public async Task<long> CreateMediaFile(MediaFileEntity mediaFile)
        {
            mediaFile.Id = await _db.Insertable(mediaFile).ExecuteReturnBigIdentityAsync();
            return mediaFile.Id;
        }



        public async Task<MediaFileEntity?> GetFileByFileId(string fileId)
        {
            return await _db.Queryable<MediaFileEntity>().Where(mf => mf.File_Id == fileId).SingleAsync();
        }

    }
}
