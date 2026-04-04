using Azure.Core;
using Dm;
using HY.ApiService.Entities;
using HY.ApiService.Enums;
using HY.ApiService.Models;
using HY.ApiService.Repositories;
using HY.ApiService.Tools;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client.Extensions.Msal;
using SkiaSharp;
using SqlSugar;
using System.Configuration;
using System.Net.Sockets;
using System.Security.Cryptography;
using StorageType = HY.ApiService.Enums.StorageType;

namespace HY.ApiService.Services
{
    public interface IMediaService
    {
        Task<UploadResult> UploadImageToLocal(long userId, IFormFile file);
        Task<UploadResult> UploadImageToCOS(long userId, IFormFile file);
        Task<UploadResult> UploadImageToOSS(long userId, IFormFile file);
        Task<UploadResult> UploadImageToS3(long userId, IFormFile file);

        Task<UploadResult> UploadHeadToLocal(long userId, IFormFile file);
        Task<UploadResult> UploadHeadToCOS(long userId, IFormFile file);
        Task<UploadResult> UploadHeadToOSS(long userId, IFormFile file);
        Task<UploadResult> UploadHeadToS3(long userId, IFormFile file);

        Task<(StorageType storageType, string path, string contentType, string? fileDownloadName)> GetMediaStorageVariantByFileId(string fileId, VariantType variantType);
    }


    public class MediaService : IMediaService
    {
        private readonly ISqlSugarClient _db;

        private readonly IConfiguration _configuration;
        private readonly IMediaStorageRepository _mediaStorageRepository;
        private readonly IMediaFileRepository _mediaFileRepository;
        private readonly IMediaStorageVariantRepository _mediaStorageVariantRepository;

        public MediaService(ISqlSugarClient db, IConfiguration configuration,IMediaStorageRepository mediaStorageRepository, IMediaFileRepository mediaFileRepository, IMediaStorageVariantRepository mediaStorageVariantRepository)
        {
            _db = db;

            _configuration = configuration;
            _mediaStorageRepository = mediaStorageRepository;
            _mediaFileRepository = mediaFileRepository;
            _mediaStorageVariantRepository = mediaStorageVariantRepository;
        }




        public async Task<UploadResult> UploadImageToLocal(long userId, IFormFile file)
        {
            var tempPath = Path.GetTempFileName();
            var filePathWithBucket = string.Empty;
            var thumbFilePathWithBucket = string.Empty;
            var compressFilePathWithBucket = string.Empty;
            Exception? e = null;
            string? fileId = null;

            var ext = Path.GetExtension(file.FileName) ?? ".jpg";
            var utcNow = DateTime.UtcNow;

            var bucket = _configuration.GetSection("MediaStorage:Local:Bucket").Value ?? "d:";
            var path = _configuration.GetSection("MediaStorage:Local:Path").Value ?? "upload";
            var basePath = Path.Combine(path, utcNow.ToString("yyyy"), utcNow.ToString("MM"), utcNow.ToString("dd"));

            MediaFileEntity mediaFileEntity;
            try
            {
                // 保存文件到临时位置并计算MD5
                var fileMD5 = await SaveAndHashAsync(tempPath, file);

                var mediaStorageEntity = await _mediaStorageRepository.GetMediaStorageByMD5(fileMD5);
                if (mediaStorageEntity != null && mediaStorageEntity.Status == 1)
                {
                    // 文件已存在且可用，增加引用计数

                    // 开启事务
                    var result = await _db.Ado.UseTranAsync(async () =>
                    {
                        mediaStorageEntity.Ref_Count++;
                        var bol = await _mediaStorageRepository.UpdateMediaStorageRefCount(mediaStorageEntity.Id, mediaStorageEntity.Ref_Count);
                        if (!bol) throw new Exception("更新引用计数失败");

                        mediaFileEntity = new MediaFileEntity
                        {
                            File_Id = Guid.NewGuid().ToString("N"),
                            Storage_Id = mediaStorageEntity.Id,
                            User_Id = userId,
                            File_Type = FileType.Image,
                            Original_Name = Path.GetFileName(file.FileName),
                            Width = null,
                            Height = null,
                            Duration = null,
                            Status = 1,
                            Created_At = DateTime.UtcNow,
                        };
                        var id = await _mediaFileRepository.CreateMediaFile(mediaFileEntity);
                        if (id <= 0) throw new Exception("创建文件记录失败");

                        fileId = mediaFileEntity.File_Id;
                    });

                    // ---------- 事务结束 ----------
                    if (!result.IsSuccess) throw new Exception($"增加引用计数失败：{result.ErrorMessage}");
                }
                else if (mediaStorageEntity != null && mediaStorageEntity.Status == 0)
                {
                    // 文件已存在但已删除，恢复文件并更新数据库记录


                    #region 将临时文件移动到目标位置

                    var fileName = $"{Guid.NewGuid():N}{ext}";

                    var filePathWithoutBucket = Path.Combine(basePath, "image", fileName);
                    filePathWithBucket = Path.Combine(bucket, filePathWithoutBucket);

                    var dir1 = Path.GetDirectoryName(filePathWithBucket);
                    if (!Directory.Exists(dir1)) Directory.CreateDirectory(dir1!);

                    File.Move(tempPath, filePathWithBucket);

                    #endregion


                    #region 获取目标缩略图

                    var thumbFileName = $"{Guid.NewGuid():N}{ext}";

                    var thumbFilePathWithoutBucket = Path.Combine(basePath, "thumb", thumbFileName);
                    thumbFilePathWithBucket = Path.Combine(bucket, thumbFilePathWithoutBucket);

                    var dir2 = Path.GetDirectoryName(thumbFilePathWithBucket);
                    if (!Directory.Exists(dir2)) Directory.CreateDirectory(dir2!);

                    var thumbLength = await CreateResizeImage(filePathWithBucket, thumbFilePathWithBucket, 150);

                    #endregion


                    #region 获取压缩图

                    var compressFileName = $"{Guid.NewGuid():N}{ext}";

                    var compressFilePathWithoutBucket = Path.Combine(basePath, "compress", compressFileName);
                    compressFilePathWithBucket = Path.Combine(bucket, compressFilePathWithoutBucket);

                    var dir3 = Path.GetDirectoryName(compressFilePathWithBucket);
                    if (!Directory.Exists(dir3)) Directory.CreateDirectory(dir3!);

                    var compressLength = await CreateResizeImage(filePathWithBucket, compressFilePathWithBucket, 800);

                    #endregion


                    // 开启事务
                    var result = await _db.Ado.UseTranAsync(async () =>
                    {
                        mediaStorageEntity.File_Path = filePathWithoutBucket;
                        mediaStorageEntity.Storage_Bucket = bucket;
                        mediaStorageEntity.Storage_Type = StorageType.Local;
                        mediaStorageEntity.Ref_Count = 1;
                        mediaStorageEntity.Status = 1;
                        var bol1 = await _mediaStorageRepository.UpdateMediaStorage(mediaStorageEntity);
                        if (!bol1) throw new Exception("恢复文件记录失败");

                        var bol2 = await _mediaStorageVariantRepository.UpdateMediaStorageVariant(mediaStorageEntity.Id, VariantType.Thumbnail, thumbFilePathWithoutBucket, file.ContentType, bucket, StorageType.Local, 1);
                        if (!bol2) throw new Exception("更新缩略图记录失败");

                        var bol3 = await _mediaStorageVariantRepository.UpdateMediaStorageVariant(mediaStorageEntity.Id, VariantType.Compress, compressFilePathWithoutBucket, file.ContentType, bucket, StorageType.Local, 1);
                        if (!bol3) throw new Exception("更新压缩图记录失败");

                        mediaFileEntity = new MediaFileEntity
                        {
                            File_Id = Guid.NewGuid().ToString("N"),
                            Storage_Id = mediaStorageEntity.Id,
                            User_Id = userId,
                            File_Type = FileType.Image,
                            Original_Name = Path.GetFileName(file.FileName),
                            Width = null,
                            Height = null,
                            Duration = null,
                            Status = 1,
                            Created_At = DateTime.UtcNow,
                        };
                        var id = await _mediaFileRepository.CreateMediaFile(mediaFileEntity);
                        if (id <= 0) throw new Exception("创建文件记录失败");

                        fileId = mediaFileEntity.File_Id;
                    });

                    // ---------- 事务结束 ----------
                    if (!result.IsSuccess) throw new Exception($"更新数据库记录失败：{result.ErrorMessage}");
                }
                else
                {
                    // 文件不存在，移动文件到目标位置并创建数据库记录


                    #region 将临时文件移动到目标位置

                    var fileName = $"{Guid.NewGuid():N}{ext}";

                    var filePathWithoutBucket = Path.Combine(basePath, "image", fileName);
                    filePathWithBucket = Path.Combine(bucket, filePathWithoutBucket);

                    var dir1 = Path.GetDirectoryName(filePathWithBucket);
                    if (!Directory.Exists(dir1)) Directory.CreateDirectory(dir1!);

                    File.Move(tempPath, filePathWithBucket);

                    #endregion


                    #region 获取目标缩略图

                    var thumbFileName = $"{Guid.NewGuid():N}{ext}";

                    var thumbFilePathWithoutBucket = Path.Combine(basePath, "thumb", thumbFileName);
                    thumbFilePathWithBucket = Path.Combine(bucket, thumbFilePathWithoutBucket);

                    var dir2 = Path.GetDirectoryName(thumbFilePathWithBucket);
                    if (!Directory.Exists(dir2)) Directory.CreateDirectory(dir2!);

                    var thumbLength = await CreateResizeImage(filePathWithBucket, thumbFilePathWithBucket, 150);

                    #endregion


                    #region 获取压缩图

                    var compressFileName = $"{Guid.NewGuid():N}{ext}";

                    var compressFilePathWithoutBucket = Path.Combine(basePath, "compress", compressFileName);
                    compressFilePathWithBucket = Path.Combine(bucket, compressFilePathWithoutBucket);

                    var dir3 = Path.GetDirectoryName(compressFilePathWithBucket);
                    if (!Directory.Exists(dir3)) Directory.CreateDirectory(dir3!);

                    var compressLength = await CreateResizeImage(filePathWithBucket, compressFilePathWithBucket, 800);

                    #endregion


                    // 开启事务
                    var result = await _db.Ado.UseTranAsync(async () =>
                    {
                        mediaStorageEntity = new MediaStorageEntity
                        {
                            File_MD5 = fileMD5,
                            File_Path = filePathWithoutBucket,
                            File_Size = file.Length,
                            Mime_Type = file.ContentType,
                            Storage_Bucket = bucket,
                            Storage_Type = StorageType.Local,
                            Ref_Count = 1,
                            Variant_Mask = (int)VariantType.Thumbnail | (int)VariantType.Compress,
                            Status = 1,
                            Created_At = DateTime.UtcNow,
                        };
                        var id1 = await _mediaStorageRepository.CreateMediaStorage(mediaStorageEntity);
                        if (id1 <= 0) throw new Exception("创建文件记录失败");

                        var thumb_mediaStorageVariantEntity = new MediaStorageVariantEntity
                        {
                            Storage_Id = mediaStorageEntity.Id,
                            Variant_Type = VariantType.Thumbnail,
                            File_Path = thumbFilePathWithoutBucket,
                            File_Size = thumbLength,
                            Mime_Type = file.ContentType,
                            Storage_Bucket = bucket,
                            Storage_Type = StorageType.Local,
                            Status = 1,
                            Created_At = DateTime.UtcNow,
                        };
                        var id2 = await _mediaStorageVariantRepository.CreateMediaStorageVariant(thumb_mediaStorageVariantEntity);
                        if (id2 <= 0) throw new Exception("创建缩略图记录失败");

                        var compress_mediaStorageVariantEntity = new MediaStorageVariantEntity
                        {
                            Storage_Id = mediaStorageEntity.Id,
                            Variant_Type = VariantType.Compress,
                            File_Path = compressFilePathWithoutBucket,
                            File_Size = compressLength,
                            Mime_Type = file.ContentType,
                            Storage_Bucket = bucket,
                            Storage_Type = StorageType.Local,
                            Status = 1,
                            Created_At = DateTime.UtcNow,
                        };
                        var id3 = await _mediaStorageVariantRepository.CreateMediaStorageVariant(compress_mediaStorageVariantEntity);
                        if (id3 <= 0) throw new Exception("创建压缩图记录失败");

                        mediaFileEntity = new MediaFileEntity
                        {
                            File_Id = Guid.NewGuid().ToString("N"),
                            Storage_Id = mediaStorageEntity.Id,
                            User_Id = userId,
                            File_Type = FileType.Image,
                            Original_Name = Path.GetFileName(file.FileName),
                            Width = null,
                            Height = null,
                            Duration = null,
                            Status = 1,
                            Created_At = DateTime.UtcNow,
                        };
                        var id = await _mediaFileRepository.CreateMediaFile(mediaFileEntity);
                        if (id <= 0) throw new Exception("创建文件记录失败");

                        fileId = mediaFileEntity.File_Id;
                    });

                    // ---------- 事务结束 ----------
                    if (!result.IsSuccess) throw new Exception($"创建数据库记录失败：{result.ErrorMessage}");
                }

            }
            catch (Exception ex)
            {
                e = ex;

                if (!string.IsNullOrEmpty(filePathWithBucket)) File.Delete(filePathWithBucket);
                if (!string.IsNullOrEmpty(thumbFilePathWithBucket)) File.Delete(thumbFilePathWithBucket);
                if (!string.IsNullOrEmpty(compressFilePathWithBucket)) File.Delete(compressFilePathWithBucket);
            }
            finally
            {
                File.Delete(tempPath);
            }

            return new UploadResult(e == null, e?.Message, fileId);
        }

        public async Task<UploadResult> UploadImageToCOS(long userId, IFormFile file)
        {
            throw new NotImplementedException();
        }

        public async Task<UploadResult> UploadImageToOSS(long userId, IFormFile file)
        {
            throw new NotImplementedException();
        }

        public async Task<UploadResult> UploadImageToS3(long userId, IFormFile file)
        {
            throw new NotImplementedException();
        }





        public async Task<UploadResult> UploadHeadToLocal(long userId, IFormFile file)
        {
            var tempPath = Path.GetTempFileName();
            var filePathWithBucket = string.Empty;
            var thumbFilePathWithBucket = string.Empty;
            Exception? e = null;
            string? fileId = null;

            var ext = Path.GetExtension(file.FileName) ?? ".jpg";
            var utcNow = DateTime.UtcNow;

            var bucket = _configuration.GetSection("MediaStorage:Local:Bucket").Value ?? "d:";
            var path = _configuration.GetSection("MediaStorage:Local:Path").Value ?? "upload";
            var basePath = Path.Combine(path, utcNow.ToString("yyyy"), utcNow.ToString("MM"), utcNow.ToString("dd"));

            MediaFileEntity mediaFileEntity;
            try
            {
                // 保存文件到临时位置并计算MD5
                var fileMD5 = await SaveAndHashAsync(tempPath, file);

                var mediaStorageEntity = await _mediaStorageRepository.GetMediaStorageByMD5(fileMD5);
                if (mediaStorageEntity != null && mediaStorageEntity.Status == 1)
                {
                    // 文件已存在且可用，增加引用计数

                    // 开启事务
                    var result = await _db.Ado.UseTranAsync(async () =>
                    {
                        mediaStorageEntity.Ref_Count++;
                        var bol = await _mediaStorageRepository.UpdateMediaStorageRefCount(mediaStorageEntity.Id, mediaStorageEntity.Ref_Count);
                        if (!bol) throw new Exception("更新引用计数失败");

                        mediaFileEntity = new MediaFileEntity
                        {
                            File_Id = Guid.NewGuid().ToString("N"),
                            Storage_Id = mediaStorageEntity.Id,
                            User_Id = userId,
                            File_Type = FileType.Image,
                            Original_Name = Path.GetFileName(file.FileName),
                            Width = null,
                            Height = null,
                            Duration = null,
                            Status = 1,
                            Created_At = DateTime.UtcNow,
                        };
                        var id = await _mediaFileRepository.CreateMediaFile(mediaFileEntity);
                        if (id <= 0) throw new Exception("创建文件记录失败");

                        fileId = mediaFileEntity.File_Id;
                    });

                    // ---------- 事务结束 ----------
                    if (!result.IsSuccess) throw new Exception($"增加引用计数失败：{result.ErrorMessage}");
                }
                else if (mediaStorageEntity != null && mediaStorageEntity.Status == 0)
                {
                    // 文件已存在但已删除，恢复文件并更新数据库记录


                    #region 将临时文件移动到目标位置

                    var fileName = $"{Guid.NewGuid():N}{ext}";

                    var filePathWithoutBucket = Path.Combine(basePath, "image", fileName);
                    filePathWithBucket = Path.Combine(bucket, filePathWithoutBucket);

                    var dir1 = Path.GetDirectoryName(filePathWithBucket);
                    if (!Directory.Exists(dir1)) Directory.CreateDirectory(dir1!);

                    File.Move(tempPath, filePathWithBucket);

                    #endregion


                    #region 获取目标头像

                    var thumbFileName = $"{Guid.NewGuid():N}{ext}";

                    var thumbFilePathWithoutBucket = Path.Combine(basePath, "thumb", thumbFileName);
                    thumbFilePathWithBucket = Path.Combine(bucket, thumbFilePathWithoutBucket);

                    var dir2 = Path.GetDirectoryName(thumbFilePathWithBucket);
                    if (!Directory.Exists(dir2)) Directory.CreateDirectory(dir2!);

                    var thumbLength = await CreateResizeImage(filePathWithBucket, thumbFilePathWithBucket, 128, 80);

                    #endregion

                    // 开启事务
                    var result = await _db.Ado.UseTranAsync(async () =>
                    {
                        mediaStorageEntity.File_Path = filePathWithoutBucket;
                        mediaStorageEntity.Storage_Bucket = bucket;
                        mediaStorageEntity.Storage_Type = StorageType.Local;
                        mediaStorageEntity.Ref_Count = 1;
                        mediaStorageEntity.Status = 1;
                        var bol1 = await _mediaStorageRepository.UpdateMediaStorage(mediaStorageEntity);
                        if (!bol1) throw new Exception("恢复文件记录失败");

                        var bol2 = await _mediaStorageVariantRepository.UpdateMediaStorageVariant(mediaStorageEntity.Id, VariantType.Thumbnail, thumbFilePathWithoutBucket, file.ContentType, bucket, StorageType.Local, 1);
                        if (!bol2) throw new Exception("更新缩略图记录失败");

                        mediaFileEntity = new MediaFileEntity
                        {
                            File_Id = Guid.NewGuid().ToString("N"),
                            Storage_Id = mediaStorageEntity.Id,
                            User_Id = userId,
                            File_Type = FileType.Image,
                            Original_Name = Path.GetFileName(file.FileName),
                            Width = null,
                            Height = null,
                            Duration = null,
                            Status = 1,
                            Created_At = DateTime.UtcNow,
                        };
                        var id = await _mediaFileRepository.CreateMediaFile(mediaFileEntity);
                        if (id <= 0) throw new Exception("创建文件记录失败");

                        fileId = mediaFileEntity.File_Id;
                    });

                    // ---------- 事务结束 ----------
                    if (!result.IsSuccess) throw new Exception($"更新数据库记录失败：{result.ErrorMessage}");
                }
                else
                {
                    // 文件不存在，移动文件到目标位置并创建数据库记录


                    #region 将临时文件移动到目标位置

                    var fileName = $"{Guid.NewGuid():N}{ext}";

                    var filePathWithoutBucket = Path.Combine(basePath, "image", fileName);
                    filePathWithBucket = Path.Combine(bucket, filePathWithoutBucket);

                    var dir1 = Path.GetDirectoryName(filePathWithBucket);
                    if (!Directory.Exists(dir1)) Directory.CreateDirectory(dir1!);

                    File.Move(tempPath, filePathWithBucket);

                    #endregion


                    #region 获取目标头像

                    var thumbFileName = $"{Guid.NewGuid():N}{ext}";

                    var thumbFilePathWithoutBucket = Path.Combine(basePath, "thumb", thumbFileName);
                    thumbFilePathWithBucket = Path.Combine(bucket, thumbFilePathWithoutBucket);

                    var dir2 = Path.GetDirectoryName(thumbFilePathWithBucket);
                    if (!Directory.Exists(dir2)) Directory.CreateDirectory(dir2!);

                    var thumbLength = await CreateResizeImage(filePathWithBucket, thumbFilePathWithBucket, 128, 80);

                    #endregion


                    // 开启事务
                    var result = await _db.Ado.UseTranAsync(async () =>
                    {
                        mediaStorageEntity = new MediaStorageEntity
                        {
                            File_MD5 = fileMD5,
                            File_Path = filePathWithoutBucket,
                            File_Size = file.Length,
                            Mime_Type = file.ContentType,
                            Storage_Bucket = bucket,
                            Storage_Type = StorageType.Local,
                            Ref_Count = 1,
                            Variant_Mask = (int)VariantType.Thumbnail,
                            Status = 1,
                            Created_At = DateTime.UtcNow,
                        };
                        var id1 = await _mediaStorageRepository.CreateMediaStorage(mediaStorageEntity);
                        if (id1 <= 0) throw new Exception("创建文件记录失败");

                        var thumb_mediaStorageVariantEntity = new MediaStorageVariantEntity
                        {
                            Storage_Id = mediaStorageEntity.Id,
                            Variant_Type = VariantType.Thumbnail,
                            File_Path = thumbFilePathWithoutBucket,
                            File_Size = thumbLength,
                            Mime_Type = file.ContentType,
                            Storage_Bucket = bucket,
                            Storage_Type = StorageType.Local,
                            Status = 1,
                            Created_At = DateTime.UtcNow,
                        };
                        var id2 = await _mediaStorageVariantRepository.CreateMediaStorageVariant(thumb_mediaStorageVariantEntity);
                        if (id2 <= 0) throw new Exception("创建缩略图记录失败");

                        mediaFileEntity = new MediaFileEntity
                        {
                            File_Id = Guid.NewGuid().ToString("N"),
                            Storage_Id = mediaStorageEntity.Id,
                            User_Id = userId,
                            File_Type = FileType.Image,
                            Original_Name = Path.GetFileName(file.FileName),
                            Width = null,
                            Height = null,
                            Duration = null,
                            Status = 1,
                            Created_At = DateTime.UtcNow,
                        };
                        var id = await _mediaFileRepository.CreateMediaFile(mediaFileEntity);
                        if (id <= 0) throw new Exception("创建文件记录失败");

                        fileId = mediaFileEntity.File_Id;
                    });

                    // ---------- 事务结束 ----------
                    if (!result.IsSuccess) throw new Exception($"创建数据库记录失败：{result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                e = ex;

                if (!string.IsNullOrEmpty(filePathWithBucket)) File.Delete(filePathWithBucket);
                if (!string.IsNullOrEmpty(thumbFilePathWithBucket)) File.Delete(thumbFilePathWithBucket);
            }
            finally
            {
                File.Delete(tempPath);
            }

            return new UploadResult(e == null, e?.Message, fileId);
        }

        public async Task<UploadResult> UploadHeadToCOS(long userId, IFormFile file)
        {
            throw new NotImplementedException();
        }

        public async Task<UploadResult> UploadHeadToOSS(long userId, IFormFile file)
        {
            throw new NotImplementedException();
        }

        public async Task<UploadResult> UploadHeadToS3(long userId, IFormFile file)
        {
            throw new NotImplementedException();
        }





        public async Task<(StorageType storageType, string path, string contentType, string? fileDownloadName)> GetMediaStorageVariantByFileId(string fileId, VariantType variantType)
        {
            if (fileId == null) throw new ArgumentNullException(nameof(fileId));

            var mediaFileEntity = await _mediaFileRepository.GetFileByFileId(fileId);

            if (mediaFileEntity == null) throw new FileNotFoundException("file not found");

            if (mediaFileEntity.File_Type != FileType.Image) throw new InvalidOperationException("file is not an image");

            //var mediaStorageEntity = await _mediaStorageRepository.GetMediaStorageById(mediaFileEntity.Storage_Id);

            //if (mediaStorageEntity == null) throw new FileNotFoundException("storage not found");

            var mediaStorageVariantEntity = await _mediaStorageVariantRepository.GetMediaStorageVariantByStorageIdAndVariantType(mediaFileEntity.Storage_Id, variantType);

            if (mediaStorageVariantEntity == null) throw new FileNotFoundException("thumbnail not found");

            switch (mediaStorageVariantEntity.Storage_Type)
            {
                case StorageType.Local:
                    {
                        var path = GetFileUrl(StorageType.Local, mediaStorageVariantEntity.Storage_Bucket, mediaStorageVariantEntity.File_Path);
                        var mimeType = mediaStorageVariantEntity.Mime_Type;
                        var fileDownloadName = Path.GetFileName(path);
                        return (StorageType.Local, path, mimeType, fileDownloadName);
                    }
                case StorageType.OSS:
                    {
                        var path = GetFileUrl(StorageType.OSS, mediaStorageVariantEntity.Storage_Bucket, mediaStorageVariantEntity.File_Path);
                        var mimeType = mediaStorageVariantEntity.Mime_Type;
                        var fileDownloadName = Path.GetFileName(path);
                        return (StorageType.OSS, path, mimeType, fileDownloadName);
                    }
                    case StorageType.COS:
                    {
                        var path = GetFileUrl(StorageType.COS, mediaStorageVariantEntity.Storage_Bucket, mediaStorageVariantEntity.File_Path);
                        var mimeType = mediaStorageVariantEntity.Mime_Type;
                        var fileDownloadName = Path.GetFileName(path);
                        return (StorageType.COS, path, mimeType, fileDownloadName);
                    }
                    case StorageType.S3:
                    {
                        var path = GetFileUrl(StorageType.S3, mediaStorageVariantEntity.Storage_Bucket, mediaStorageVariantEntity.File_Path);
                        var mimeType = mediaStorageVariantEntity.Mime_Type;
                        var fileDownloadName = Path.GetFileName(path);
                        return (StorageType.S3, path, mimeType, fileDownloadName);
                    }
                default:
                    throw new Exception("unsupported storage type");
            }
        }






        private string GetFileUrl(StorageType storageType, string bucket, string path)
        {
            return storageType switch
            {
                StorageType.Local => Path.Combine(bucket, path),
                StorageType.OSS => $"https://{bucket}.oss-cn-shanghai.aliyuncs.com/{path}",
                StorageType.COS => $"https://{bucket}.cos.ap-guangzhou.myqcloud.com/{path}",
                StorageType.S3 => $"https://{bucket}.blob.core.windows.net/{path}",
                _ => throw new Exception("unknown storage type")
            };
        }

        private const int BufferSize = 81920; // .NET推荐

        private async Task<string> SaveAndHashAsync(string savePath ,IFormFile file, CancellationToken ct = default)
        {
            if (file == null || file.Length == 0) throw new ArgumentException("file is null or empty");

            using var md5 = MD5.Create();
            using var input = file.OpenReadStream();
            using var output = File.Create(savePath);

            var buffer = new byte[BufferSize];
            int read;

            while ((read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
            {
                await output.WriteAsync(buffer.AsMemory(0, read), ct);      // 写入文件
                md5.TransformBlock(buffer, 0, read, null, 0);               // 计算 MD5
            }

            md5.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

            return Convert.ToHexString(md5.Hash!).ToLowerInvariant();
        }

        private async Task<long> CreateResizeImage(string inputPath, string outputPath, int? width = null, int? quality = null)
        {
            using var input = File.OpenRead(inputPath);
            using var original = SKBitmap.Decode(input);

            if (original == null) throw new ArgumentNullException("图片解析失败");

            width ??= original.Width;
            quality ??= 80;

            var ratio = (float)width / original.Width;
            var height = (int)(original.Height * ratio);

            using var resized = original.Resize(new SKImageInfo(width.Value, height), SKFilterQuality.Medium);

            using var image = SKImage.FromBitmap(resized);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality.Value);

            using var output = File.OpenWrite(outputPath);
            data.SaveTo(output);

            return output.Length;
        }

    }
}
