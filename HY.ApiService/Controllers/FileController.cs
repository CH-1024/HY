using HY.ApiService.Enums;
using HY.ApiService.Models;
using HY.ApiService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OracleInternal.Secure.Network;
using SkiaSharp;
using System.Security.Claims;
using System.Security.Cryptography;

namespace HY.ApiService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FileController : ControllerBase
    {
        readonly IMediaService _mediaService;
        readonly IConfiguration _configuration;

        public FileController(IMediaService mediaService, IConfiguration configuration)
        {
            _mediaService = mediaService;
            _configuration = configuration;
        }



        [Authorize]
        [HttpPost("upload/image")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            var type = _configuration.GetSection("MediaStorage:Local:Type").Value ?? "Local";

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var userId = long.Parse(userIdStr!);

            UploadResult result;

            if (file == null)
            {
                result = new UploadResult(false, "File is required");
            }
            else if (type == "Local")
            {
                 result = await _mediaService.UploadImageToLocal(userId, file);
            }
            else if (type == "OSS")
            {
                result = await _mediaService.UploadImageToOSS(userId, file);
            }
            else if (type == "COS")
            {
                result = await _mediaService.UploadImageToCOS(userId, file);
            }
            else if (type == "S3")
            {
                result = await _mediaService.UploadImageToS3(userId, file);
            }
            else
            {
                result = new UploadResult(false, "Unsupported media storage type");
            }

            return Ok(new Response(result.IsSucc, result.Error)
            {
                Data = new Dictionary<string, object?>
                {
                    { "File_Id", result.FileId },
                }
            });
        }

        [Authorize]
        [HttpPost("upload/video")]
        public async Task<IActionResult> UploadVideo(IFormFile file)
        {
            var type = _configuration.GetSection("MediaStorage:Local:Type").Value ?? "Local";

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var userId = long.Parse(userIdStr!);

            UploadResult result;

            if (file == null)
            {
                result = new UploadResult(false, "File is required");
            }
            else if (type == "Local")
            {
                result = await _mediaService.UploadVideoToLocal(userId, file);
            }
            else if (type == "OSS")
            {
                result = await _mediaService.UploadVideoToOSS(userId, file);
            }
            else if (type == "COS")
            {
                result = await _mediaService.UploadVideoToCOS(userId, file);
            }
            else if (type == "S3")
            {
                result = await _mediaService.UploadVideoToS3(userId, file);
            }
            else
            {
                result = new UploadResult(false, "Unsupported media storage type");
            }

            return Ok(new Response(result.IsSucc, result.Error)
            {
                Data = new Dictionary<string, object?>
                {
                    { "File_Id", result.FileId },
                }
            });
        }

        [Authorize]
        [HttpPost("upload/head")]
        public async Task<IActionResult> UploadHead(IFormFile file)
        {
            var type = _configuration.GetSection("MediaStorage:Local:Type").Value ?? "Local";

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var userId = long.Parse(userIdStr!);

            UploadResult result;

            if (file == null)
            {
                result = new UploadResult(false, "File is required");
            }
            else if (type == "Local")
            {
                result = await _mediaService.UploadHeadToLocal(userId, file);
            }
            else if (type == "OSS")
            {
                result = await _mediaService.UploadHeadToOSS(userId, file);
            }
            else if (type == "COS")
            {
                result = await _mediaService.UploadHeadToCOS(userId, file);
            }
            else if (type == "S3")
            {
                result = await _mediaService.UploadHeadToS3(userId, file);
            }
            else
            {
                result = new UploadResult(false, "Unsupported media storage type");
            }

            return Ok(new Response(result.IsSucc, result.Error)
            {
                Data = new Dictionary<string, object?>
                {
                    { "File_Id", result.FileId },
                }
            });
        }








        [HttpGet("image/origin/{fileId}")]
        public async Task<IActionResult> GetOriginImage(string fileId)
        {
            var file = await _mediaService.GetMediaStorageByFileId(fileId);

            return file.storageType switch
            {
                StorageType.Local => PhysicalFile(file.path, file.contentType, file.fileDownloadName, true),
                StorageType.OSS => Redirect(file.path),
                StorageType.COS => Redirect(file.path),
                StorageType.S3 => Redirect(file.path),
                _ => NotFound()
            };
        }


        [HttpGet("image/compress/{fileId}")]
        public async Task<IActionResult> GetCompressImage(string fileId)
        {
            var file = await _mediaService.GetMediaStorageVariantByFileId(fileId, VariantType.Compress);

            return file.storageType switch
            {
                StorageType.Local => PhysicalFile(file.path, file.contentType, file.fileDownloadName, true),
                StorageType.OSS => Redirect(file.path),
                StorageType.COS => Redirect(file.path),
                StorageType.S3 => Redirect(file.path),
                _ => NotFound()
            };
        }


        [HttpGet("image/thumb/{fileId}")]
        public async Task<IActionResult> GetThumbImage(string fileId)
        {
            var file = await _mediaService.GetMediaStorageVariantByFileId(fileId, VariantType.Thumbnail);

            return file.storageType switch
            {
                StorageType.Local => PhysicalFile(file.path, file.contentType, file.fileDownloadName, true),
                StorageType.OSS => Redirect(file.path),
                StorageType.COS => Redirect(file.path),
                StorageType.S3 => Redirect(file.path),
                _ => NotFound()
            };
        }




        [HttpGet("video/origin/{fileId}")]
        public async Task<IActionResult> GetOriginVideo(string fileId)
        {
            var file = await _mediaService.GetMediaStorageByFileId(fileId);

            return file.storageType switch
            {
                StorageType.Local => PhysicalFile(file.path, file.contentType, file.fileDownloadName, true),
                StorageType.OSS => Redirect(file.path),
                StorageType.COS => Redirect(file.path),
                StorageType.S3 => Redirect(file.path),
                _ => NotFound()
            };
        }


        [HttpGet("video/compress/{fileId}")]
        public async Task<IActionResult> GetCompressVideo(string fileId)
        {
            var file = await _mediaService.GetMediaStorageVariantByFileId(fileId, VariantType.LowVideo);

            return file.storageType switch
            {
                StorageType.Local => PhysicalFile(file.path, file.contentType, file.fileDownloadName, true),
                StorageType.OSS => Redirect(file.path),
                StorageType.COS => Redirect(file.path),
                StorageType.S3 => Redirect(file.path),
                _ => NotFound()
            };
        }


        [HttpGet("video/cover/{fileId}")]
        public async Task<IActionResult> GetCoverVideo(string fileId)
        {
            var file = await _mediaService.GetMediaStorageVariantByFileId(fileId, VariantType.Cover);

            return file.storageType switch
            {
                StorageType.Local => PhysicalFile(file.path, file.contentType, file.fileDownloadName, true),
                StorageType.OSS => Redirect(file.path),
                StorageType.COS => Redirect(file.path),
                StorageType.S3 => Redirect(file.path),
                _ => NotFound()
            };
        }





        [HttpGet("head/{fileId}")]
        public async Task<IActionResult> GetHead(string fileId)
        {
            var file = await _mediaService.GetMediaStorageVariantByFileId(fileId, VariantType.Thumbnail);

            Response.Headers.CacheControl = "public, max-age=31536000, immutable";      // 开启强缓存

            return file.storageType switch
            {
                StorageType.Local => PhysicalFile(file.path, file.contentType, file.fileDownloadName, true),
                StorageType.OSS => Redirect(file.path),
                StorageType.COS => Redirect(file.path),
                StorageType.S3 => Redirect(file.path),
                _ => NotFound()
            };
        }

    }
}
