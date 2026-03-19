using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace HY.MAUI.Communication
{
    public class ProgressableStreamContent : HttpContent
    {
        private readonly Stream _content;
        private readonly int _bufferSize;
        private readonly IProgress<double>? _progress;
        private readonly long _contentLength;

        public ProgressableStreamContent(Stream content, int bufferSize, IProgress<double>? progress)
        {
            _content = content;
            _bufferSize = bufferSize;
            _progress = progress;
            _contentLength = content.Length;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            var buffer = new byte[_bufferSize];
            long uploaded = 0;

            int read;
            while ((read = await _content.ReadAsync(buffer)) > 0)
            {
                await stream.WriteAsync(buffer.AsMemory(0, read));
                uploaded += read;

                _progress?.Report((double)uploaded / _contentLength * 100);
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _contentLength;
            return true;
        }
    }
}
