using Android.Graphics;
using Android.Hardware.Lights;
using AndroidX.Camera.Core;
using AndroidX.Camera.Core.ResolutionSelector;
using AndroidX.Camera.Lifecycle;
using AndroidX.Core.Content;
using AndroidX.Lifecycle;
using Bumptech.Glide.Util;
using CommunityToolkit.Maui.Core;
using HY.MAUI.Services.Interfaces;
using Java.Lang;
using Java.Nio;
using Java.Util.Concurrent;
using Microsoft.Maui;
using SIPSorcery.Media;
using SIPSorceryMedia.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using static Android.Graphics.Bitmap;
using static Android.Icu.Text.ListFormatter;
using static Android.Provider.MediaStore;
using static AndroidX.Media3.Container.ObuParser;
using Exception = Java.Lang.Exception;
using Executors = Java.Util.Concurrent.Executors;
using Size = Android.Util.Size;

namespace HY.MAUI.Platforms.Android
{
    public class VideoRecorderService : IVideoRecorderService
    {
        ImageAnalysis _imageAnalysis;
        ProcessCameraProvider _cameraProvider;
        FrameAnalyzer _frameAnalyzer;


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

            var cameraProviderFuture = ProcessCameraProvider.GetInstance(Platform.AppContext);

            var cameraProvider = (ProcessCameraProvider)cameraProviderFuture.Get();
            if (cameraProvider.AvailableCameraInfos.Count == 0)
            {
                return await StartTesting();
            }

            _cameraProvider = cameraProvider;

            //// 配置预览
            //var preview = new Preview.Builder().Build();
            //preview.SetSurfaceProvider(previewView.SurfaceProvider);

            // 配置帧分析
            _imageAnalysis = new ImageAnalysis.Builder()
                .SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest) // 只保留最新帧
                .SetOutputImageFormat(ImageAnalysis.OutputImageFormatRgba8888) // 设置输出格式
                .Build();

            _frameAnalyzer = new FrameAnalyzer(this);
            _imageAnalysis.SetAnalyzer(Executors.NewSingleThreadExecutor(), _frameAnalyzer);

            var lifecycleOwner = (ILifecycleOwner)Platform.CurrentActivity;
            var cameraSelector = CameraSelector.DefaultFrontCamera;

            _cameraProvider.UnbindAll();

            // 绑定帧分析
            _cameraProvider.BindToLifecycle(lifecycleOwner, cameraSelector/*, preview*/, _imageAnalysis);

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
                await _cameraProvider.ShutdownAsync().GetAsync();
            }

            testSource?.Dispose();
            testSource = null;

            _cameraProvider?.UnbindAll();

            _frameAnalyzer?.Dispose();
            _frameAnalyzer = null;

            _imageAnalysis?.ClearAnalyzer();
            _imageAnalysis?.Dispose();
            _imageAnalysis = null;

            _cameraProvider?.Dispose();
            _cameraProvider = null;

            _isStarting = false;
            _isTesting = false;

            return true;
        }







        // 自定义帧分析器
        public class FrameAnalyzer : Java.Lang.Object, ImageAnalysis.IAnalyzer
        {
            VideoRecorderService _recorderService;

            IVideoEncoder _videoEncoder => _recorderService._videoEncoder;
            Action<uint, byte[]> OnVideoSourceEncodedSample => _recorderService.OnVideoSourceEncodedSample;
            Action<uint, int, int, byte[], VideoPixelFormatsEnum> OnVideoSourceRawSample => _recorderService.OnVideoSourceRawSample;
            MediaFormatManager<VideoFormat> _formatManager => _recorderService._formatManager;


            public Size? DefaultTargetResolution => null;   // new Size(1280, 720)

            public FrameAnalyzer(VideoRecorderService recorderService)
            {
                _recorderService = recorderService;
            }


            DateTime _lastFrameAt = DateTime.MinValue;

            public void Analyze(IImageProxy imageProxy)
            {
                using (imageProxy)
                {
                    try
                    {
                        var rotation = imageProxy.ImageInfo.RotationDegrees; // 获取设备方向

                        using var bitmap = imageProxy.ToBitmap();
                        using var newBitmap = ApplyRotation(bitmap, rotation);

                        var w = newBitmap.Width;
                        var h = newBitmap.Height;

                        // 确保 Bitmap 是 RGBA8888
                        // RGBA8888 = 4 bytes / pixel
                        var rgba = new byte[w * h * 4];

                        using (var buffer = ByteBuffer.Allocate(rgba.Length))
                        {
                            newBitmap.CopyPixelsToBuffer(buffer);

                            // 回收 Bitmap
                            newBitmap.Recycle();

                            buffer.Rewind();
                            buffer.Get(rgba);
                        }

                        // RGBA -> I420
                        var i420 = PixelConverter.RGBAtoI420(rgba, w, h, w * 4);

                        if (OnVideoSourceEncodedSample != null && _videoEncoder != null && !_formatManager.SelectedFormat.IsEmpty())
                        {
                            lock (_videoEncoder)
                            {
                                var encodedBuffer = _videoEncoder?.EncodeVideo(w, h, i420, VideoPixelFormatsEnum.I420, _formatManager.SelectedFormat.Codec);
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

                            OnVideoSourceRawSample?.Invoke(frameSpacing, w, h, i420, VideoPixelFormatsEnum.I420);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                    finally
                    {
                        // 必须关闭以释放资源
                        imageProxy.Close();
                    }
                }

                _lastFrameAt = DateTime.Now;
            }

            private Bitmap ApplyRotation(Bitmap sourceBitmap, int rotationDegrees)
            {
                if (sourceBitmap == null) return sourceBitmap;

                Matrix matrix = new Matrix();
                switch (rotationDegrees)
                {
                    case 90:
                        matrix.PostRotate(90);
                        matrix.PostScale(-1, 1);
                        break;
                    case 180:
                        matrix.PostRotate(180);
                        break;
                    case 270:
                        matrix.PostRotate(270);
                        matrix.PostScale(-1, 1);
                        break;
                }

                Bitmap rotatedBitmap = Bitmap.CreateBitmap(
                    sourceBitmap,
                    0, 0,                  // 裁剪起点 (x, y)
                    sourceBitmap.Width,     // 源图像的宽度（不要修改）
                    sourceBitmap.Height,    // 源图像的高度（不要修改）
                    matrix,                 // 旋转矩阵（会自动处理尺寸）
                    true                    // 启用抗锯齿
                );

                // 回收 Bitmap
                sourceBitmap.Recycle();

                return rotatedBitmap;
            }


        }

    }
}
