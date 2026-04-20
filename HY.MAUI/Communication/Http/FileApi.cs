using HY.MAUI.Communication.Requests;
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


        public async Task<Response?> UploadHead(FileResult? fileResult)
        {
            if (fileResult == null) return null;

            using var stream = await fileResult.OpenReadAsync();

            var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(fileResult.ContentType);

            var content = new MultipartFormDataContent
            {
                { streamContent, "file", fileResult.FileName },
            };

            return await PostAsync(ApiUrl.UploadHead, content);
        }


        public async Task<Response?> UploadImage(FileResult? fileResult, IProgress<double> progress)
        {
            if (fileResult == null) return null;

            using var stream = await fileResult.OpenReadAsync();

            var progressContent = new ProgressableStreamContent(stream, 81920, progress);

            progressContent.Headers.ContentType = new MediaTypeHeaderValue(fileResult.ContentType);

            var content = new MultipartFormDataContent
            {
                { progressContent, "file", fileResult.FileName },
            };

            return await PostAsync(ApiUrl.UploadImage, content);
        }


        public async Task<Response?> UploadVideo(FileResult? fileResult, IProgress<double> progress)
        {
            if (fileResult == null) return null;

            using var stream = await fileResult.OpenReadAsync();

            var progressContent = new ProgressableStreamContent(stream, 81920, progress);

            progressContent.Headers.ContentType = new MediaTypeHeaderValue(fileResult.ContentType);

            var content = new MultipartFormDataContent
            {
                { progressContent, "file", fileResult.FileName },
            };

            return await PostAsync(ApiUrl.UploadVideo, content);
        }

    }
}
