using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;

namespace HY.MAUI.Communication.Http
{
    public class FileApi : BaseApi
    {
        public FileApi(HttpClient http) : base(http)
        {

        }


        public async Task<Response?> UploadHead(FileResult? fileResult, CancellationToken token = default)
        {
            if (fileResult == null) return null;

            await using var stream = await fileResult.OpenReadAsync();

            var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(fileResult.ContentType ?? "application/octet-stream");

            using var content = new MultipartFormDataContent();
            content.Add(streamContent, "file", fileResult.FileName);

            return await PostAsync(ApiUrl.UploadHead, content, token);
        }

        public async Task<Response?> UploadImage(FileResult? fileResult, CancellationToken token = default)
        {
            if (fileResult == null) return null;

            await using var stream = await fileResult.OpenReadAsync();

            var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(fileResult.ContentType ?? "application/octet-stream");

            using var content = new MultipartFormDataContent();
            content.Add(streamContent, "file", fileResult.FileName);

            return await PostAsync(ApiUrl.UploadImage, content, token);
        }

        public async Task<Response?> UploadVideo(FileResult? fileResult, CancellationToken token = default)
        {
            if (fileResult == null) return null;

            await using var stream = await fileResult.OpenReadAsync();

            var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(fileResult.ContentType ?? "application/octet-stream");

            using var content = new MultipartFormDataContent();
            content.Add(streamContent, "file", fileResult.FileName);

            return await PostAsync(ApiUrl.UploadVideo, content, token);
        }

    }
}
