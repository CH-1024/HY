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
            if (file == null) return BadRequest(new Response(false, "File is required"));

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!long.TryParse(userIdStr, out var userId)) return Unauthorized();

            var type = _configuration.GetSection("MediaStorage:Local:Type").Value ?? "Local";

            UploadResult result;

            if (type == "Local")
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
        [HttpPost("upload/head")]
        public async Task<IActionResult> UploadHead(IFormFile file)
        {
            if (file == null) return BadRequest(new Response(false, "File is required"));

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!long.TryParse(userIdStr, out var userId)) return Unauthorized();

            var type = _configuration.GetSection("MediaStorage:Local:Type").Value ?? "Local";

            UploadResult result;

            if (type == "Local")
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


        [HttpGet("thumb/{fileId}")]
        public async Task<IActionResult> GetThumb(string fileId)
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


        [HttpGet("compress/{fileId}")]
        public async Task<IActionResult> GetCompress(string fileId)
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


    }
}
