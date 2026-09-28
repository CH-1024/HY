using HY.MAUI.Services.Interfaces;
using Microsoft.UI.Xaml.Controls;
using Org.BouncyCastle.Utilities.Encoders;
using SIPSorcery.Media;
using SIPSorceryMedia.Abstractions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using Vpx.Net;
using Windows.Devices.Enumeration;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;
using WinRT;

namespace HY.MAUI.Platforms.Windows
{
    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }

    public class VideoRecorderService : IVideoRecorderService
    {
        MediaCapture _mediaCapture;
        MediaFrameReader _frameReader;


        VideoTestPatternSource? testSource;
        IVideoEncoder _videoEncoder;
        MediaFormatManager<VideoFormat> _formatManager;

        bool _isStarting { get; set; }
        bool _isTesting { get; set; }


        public bool IsStarting => _isStarting || _isTesting;

        public event Action<uint, byte[]> OnVideoSourceEncodedSample;
        public event Action<byte[], uint, uint, VideoPixelFormatsEnum> OnVideoSinkDecodedSample;
        public event Action<uint, int, int, byte[], VideoPixelFormatsEnum> OnVideoSourceRawSample;


        public void SetVideoEncoder(IVideoEncoder encoder)
        {
            _videoEncoder = encoder;
            _formatManager = new MediaFormatManager<VideoFormat>(IVideoRecorderService.SupportedFormats);

            // 默认VP8
            _formatManager.SetSelectedFormat(IVideoRecorderService.SupportedFormats.Find(x => x.Codec == VideoCodecsEnum.VP8));
        }

        public void RestrictFormats(Func<VideoFormat, bool> filter)
        {
            _formatManager.RestrictFormats(filter);
        }

        public void SetVideoFormat(VideoFormat format)
        {
            _formatManager.SetSelectedFormat(format);
        }

        public VideoFormat GetSelectedFormat()
        {
            return _formatManager.SelectedFormat;
        }

        public List<VideoFormat> GetVideoFormats()
        {
            return _formatManager.GetSourceFormats();
        }


        public void GotVideoFrame(IPEndPoint remoteEndPoint, uint timestamp, byte[] payload, VideoFormat format)
        {
            if (_isStarting || _isTesting)
            {
                var decodedFrames = _videoEncoder.DecodeVideo(payload, VideoPixelFormatsEnum.I420, VideoCodecsEnum.VP8) ?? [];
                foreach (var decodedFrame in decodedFrames)
                {
                    OnVideoSinkDecodedSample(decodedFrame.Sample, decodedFrame.Width, decodedFrame.Height, VideoPixelFormatsEnum.Bgr);
                }
            }
        }


        public async Task<bool> StartTesting()
        {
            if (_isStarting || _isTesting) return false;

            testSource = new VideoTestPatternSource(_videoEncoder);
            testSource.SetVideoSourceFormat(testSource.GetVideoSourceFormats().Find(x => x.Codec == VideoCodecsEnum.VP8));

            testSource.OnVideoSourceEncodedSample += (dur, sample) => OnVideoSourceEncodedSample?.Invoke(dur, sample);
            testSource.OnVideoSourceRawSample += (dur, width, height, sample, format) =>
            {
                var i420 = PixelConverter.BGRtoI420(sample, width, height, width * 3);
                OnVideoSourceRawSample?.Invoke(dur, width, height, i420, format);
            };

            await testSource.StartVideo();

            _isTesting = true;

            return true;
        }

        public async Task<bool> StartRecording()
        {
            if (_isStarting || _isTesting) return false;

            // 1. 初始化 MediaCapture 对象
            _mediaCapture = new MediaCapture();

            var videos = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            if (videos.Count == 0)
            {
                return await StartTesting();
            }

            var settings = new MediaCaptureInitializationSettings()
            {
                MemoryPreference = MediaCaptureMemoryPreference.Cpu,
                StreamingCaptureMode = StreamingCaptureMode.Video,
                MediaCategory = MediaCategory.Communications,
            };
            await _mediaCapture.InitializeAsync(settings);

            // 配置视频帧读取器
            var frameSource = _mediaCapture.FrameSources.Values.FirstOrDefault(source => source.Info.MediaStreamType == MediaStreamType.VideoRecord);
            _frameReader = await _mediaCapture.CreateFrameReaderAsync(frameSource, MediaEncodingSubtypes.Nv12, new BitmapSize(320, 320));
            _frameReader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Realtime;
            _frameReader.FrameArrived += FrameReader_FrameArrived;

            await _frameReader.StartAsync();

            _isStarting = true;

            return true;
        }

        public async Task<bool> StopRecording()
        {
            if (!(_isStarting || _isTesting)) return false;

            if (_isTesting)
            {
                await testSource.CloseVideo();
            }
            else
            {
                await _frameReader?.StopAsync();
            }

            testSource?.Dispose();
            testSource = null;

            _frameReader?.FrameArrived -= FrameReader_FrameArrived;

            _frameReader?.Dispose();
            _mediaCapture?.Dispose();

            _frameReader = null;
            _mediaCapture = null;

            _isStarting = false;
            _isTesting = false;

            return true;
        }












        private SoftwareBitmap _backBuffer;
        private DateTime _lastFrameAt = DateTime.MinValue;

        private async void FrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            using (var mediaFrameReference = sender.TryAcquireLatestFrame())
            {
                var videoMediaFrame = mediaFrameReference?.VideoMediaFrame;
                var softwareBitmap = videoMediaFrame?.SoftwareBitmap;

                if (softwareBitmap == null && videoMediaFrame != null)
                {
                    var videoFrame = videoMediaFrame.GetVideoFrame();
                    softwareBitmap = await SoftwareBitmap.CreateCopyFromSurfaceAsync(videoFrame.Direct3DSurface);
                }

                if (softwareBitmap != null)
                {
                    int width = softwareBitmap.PixelWidth;
                    int height = softwareBitmap.PixelHeight;

                    if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Nv12)
                    {
                        softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Nv12, BitmapAlphaMode.Ignore);
                    }

                    // Swap the processed frame to _backBuffer and dispose of the unused image.
                    softwareBitmap = Interlocked.Exchange(ref _backBuffer, softwareBitmap);

                    using (BitmapBuffer buffer = _backBuffer.LockBuffer(BitmapBufferAccessMode.Read))
                    {
                        using (var reference = buffer.CreateReference())
                        {
                            unsafe
                            {
                                byte* dataInBytes;
                                uint capacity;
                                reference.As<IMemoryBufferByteAccess>().GetBuffer(out dataInBytes, out capacity);
                                byte[] nv12Buffer = new byte[capacity];
                                Marshal.Copy((IntPtr)dataInBytes, nv12Buffer, 0, (int)capacity);

                                var i420 = PixelConverter.NV12toI420(nv12Buffer, width, height);

                                if (OnVideoSourceEncodedSample != null && _videoEncoder != null && !_formatManager.SelectedFormat.IsEmpty())
                                {
                                    lock (_videoEncoder)
                                    {
                                        var encodedBuffer = _videoEncoder?.EncodeVideo(width, height, i420, VideoPixelFormatsEnum.I420, _formatManager.SelectedFormat.Codec);
                                        if (encodedBuffer != null)
                                        {
                                            uint durationRtpUnits = 90000 / 30;
                                            OnVideoSourceEncodedSample?.Invoke(durationRtpUnits, encodedBuffer);
                                        }
                                    }
                                }

                                if (OnVideoSourceRawSample != null)
                                {
                                    uint frameSpacing = 0;
                                    if (_lastFrameAt != DateTime.MinValue)
                                    {
                                        frameSpacing = Convert.ToUInt32(DateTime.Now.Subtract(_lastFrameAt).TotalMilliseconds);
                                    }

                                    OnVideoSourceRawSample?.Invoke(frameSpacing, width, height, i420, VideoPixelFormatsEnum.I420);
                                }
                            }
                        }
                    }

                    _backBuffer?.Dispose();
                    softwareBitmap?.Dispose();
                }

                _lastFrameAt = DateTime.Now;
            }
        }

    }
}
