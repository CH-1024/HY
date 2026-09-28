using SIPSorceryMedia.Abstractions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace HY.MAUI.Services.Interfaces
{
    public interface IVideoRecorderService
    {
        protected static readonly List<VideoFormat> SupportedFormats = new List<VideoFormat>
        {
            new VideoFormat(VideoCodecsEnum.VP8, 96, 90000),
            //new VideoFormat(VideoCodecsEnum.H264, 100, 90000, "packetization-mode=1"),
            //new VideoFormat(VideoCodecsEnum.AV1, 101, 90000)
        };



        bool IsStarting { get; }



        event Action<uint, byte[]> OnVideoSourceEncodedSample;
        event Action<uint, int, int, byte[], VideoPixelFormatsEnum> OnVideoSourceRawSample;
        event Action<byte[], uint, uint, VideoPixelFormatsEnum> OnVideoSinkDecodedSample;

        void SetVideoEncoder(IVideoEncoder encoder);
        void SetVideoFormat(VideoFormat format);
        VideoFormat GetSelectedFormat();
        List<VideoFormat> GetVideoFormats();

        void GotVideoFrame(IPEndPoint remoteEndPoint, uint timestamp, byte[] payload, VideoFormat format);

        Task<bool> StartTesting();
        Task<bool> StartRecording();
        Task<bool> StopRecording();



        byte[] MirrorI420(byte[] src, int width, int height)
        {
            int ySize = width * height;
            int uvWidth = width / 2;
            int uvHeight = height / 2;
            int uvSize = uvWidth * uvHeight;

            byte[] dst = new byte[src.Length];

            // Y 平面
            for (int y = 0; y < height; y++)
            {
                int srcRow = y * width;
                int dstRow = y * width;

                for (int x = 0; x < width; x++)
                {
                    dst[dstRow + x] = src[srcRow + (width - 1 - x)];
                }
            }

            // U 平面
            int uOffset = ySize;

            for (int y = 0; y < uvHeight; y++)
            {
                int srcRow = uOffset + y * uvWidth;
                int dstRow = uOffset + y * uvWidth;

                for (int x = 0; x < uvWidth; x++)
                {
                    dst[dstRow + x] = src[srcRow + (uvWidth - 1 - x)];
                }
            }

            // V 平面
            int vOffset = ySize + uvSize;

            for (int y = 0; y < uvHeight; y++)
            {
                int srcRow = vOffset + y * uvWidth;
                int dstRow = vOffset + y * uvWidth;

                for (int x = 0; x < uvWidth; x++)
                {
                    dst[dstRow + x] = src[srcRow + (uvWidth - 1 - x)];
                }
            }

            return dst;
        }

    }

}
