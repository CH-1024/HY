using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace HY.MAUI.Communication
{
    public class ProgressableStreamContent : HttpContent
    {
        private readonly Stream _stream;
        private readonly int _bufferSize;
        private readonly IProgress<double>? _progress;
        private readonly long _length;

        public ProgressableStreamContent(Stream stream, int bufferSize, IProgress<double>? progress)
        {
            _stream = stream;
            _bufferSize = bufferSize;
            _progress = progress;
            _length = stream.CanSeek ? stream.Length : -1;
        }

        protected override async Task SerializeToStreamAsync(Stream target, TransportContext? context)
        {
            var buffer = new byte[_bufferSize];
            long uploaded = 0;

            int read;
            while ((read = await _stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await target.WriteAsync(buffer, 0, read);

                uploaded += read;

                if (_length > 0)
                {
                    _progress?.Report((double)uploaded / _length);
                }
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            if (_length < 0)
            {
                length = 0;
                return false;
            }

            length = _length;
            return true;
        }
    }
}
