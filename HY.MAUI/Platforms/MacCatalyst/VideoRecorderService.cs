using AVFoundation;
using CoreFoundation;
using CoreMedia;
using CoreVideo;
using Foundation;
using HY.MAUI.Services.Interfaces;
using SIPSorcery.Media;
using SIPSorceryMedia.Abstractions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace HY.MAUI.Platforms.MacCatalyst
{
    public class VideoRecorderService : IVideoRecorderService
    {
        AVCaptureSession _captureSession;
        AVCaptureDeviceInput _deviceInput;
        AVCaptureVideoDataOutput _videoOutput;

        DispatchQueue? _captureQueue;
        AVCaptureVideoDataDelegate? _delegate;


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
            if (_isStarting) return false;

            if (!await AVCaptureDevice.RequestAccessForMediaTypeAsync(AVAuthorizationMediaType.Video)) return false;


            #region 获取可用摄像头

            // 直接获取所有 Video Capture Device
            // var devices = AVCaptureDevice.DevicesWithMediaType("vide");

            var deviceTypes = new AVCaptureDeviceType[]
            {
                    AVCaptureDeviceType.BuiltInWideAngleCamera,
                    AVCaptureDeviceType.External,
                    AVCaptureDeviceType.ContinuityCamera
            };
            var discoverySession = AVCaptureDeviceDiscoverySession.Create(deviceTypes, AVMediaTypes.Video, AVCaptureDevicePosition.Unspecified);
            var device = discoverySession.Devices.FirstOrDefault() ?? AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Video);
            if (device == null)
            {
                return await StartTesting();
            }

            #endregion


            _captureSession = new AVCaptureSession();

            _captureSession.BeginConfiguration();

            try
            {
                // 设置分辨率
                _captureSession.SessionPreset = AVCaptureSession.PresetLow;

                #region 设置帧率
                if (device.LockForConfiguration(out var error))
                {
                    try
                    {
                        device.ActiveVideoMinFrameDuration = new CMTime(1, 30);
                        device.ActiveVideoMaxFrameDuration = new CMTime(1, 30);
                    }
                    finally
                    {
                        device.UnlockForConfiguration();
                    }
                }
                #endregion

                _deviceInput = AVCaptureDeviceInput.FromDevice(device, out var inputError);
                _captureSession.AddInput(_deviceInput);

                // 配置视频输出
                _videoOutput = new AVCaptureVideoDataOutput
                {
                    AlwaysDiscardsLateVideoFrames = true,
                    WeakVideoSettings = new NSDictionary(CVPixelBuffer.PixelFormatTypeKey, (int)CVPixelFormatType.CV32BGRA),
                };
                _captureSession.AddOutput(_videoOutput);

                _captureQueue = new DispatchQueue("NativeCaptureVideo.MacCatalyst");
                _delegate = new AVCaptureVideoDataDelegate(this);
                _videoOutput.SetSampleBufferDelegate(_delegate, _captureQueue);

                foreach (var connection in _videoOutput.Connections)
                {
                    // 设置视频方向
                    if (connection.SupportsVideoOrientation)
                    {
                        connection.VideoOrientation = AVCaptureVideoOrientation.Portrait;
                    }

                    // 设置视频镜像
                    if (connection.SupportsVideoMirroring)
                    {
                        connection.VideoMirrored = true;
                    }
                }
            }
            finally
            {
                _captureSession.CommitConfiguration();
            }

            // 开始捕获
            _captureSession.StartRunning();

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
                _captureSession?.StopRunning();
            }

            testSource?.Dispose();
            testSource = null;

            _videoOutput?.SetSampleBufferDelegate(null, null);
            _videoOutput?.Dispose();
            _videoOutput = null;

            _delegate?.Dispose();
            _delegate = null;

            _deviceInput?.Dispose();
            _deviceInput = null;

            foreach (var _input in _captureSession.Inputs)
            {
                _captureSession.RemoveInput(_input);
                _input.Dispose();
            }

            _captureSession?.Dispose();
            _captureSession = null;

            _isStarting = false;
            _isTesting = false;

            return true;
        }







        // 内部类处理帧数据
        private class AVCaptureVideoDataDelegate : AVCaptureVideoDataOutputSampleBufferDelegate
        {
            VideoRecorderService _recorderService;

            IVideoEncoder _videoEncoder => _recorderService._videoEncoder;
            Action<uint, byte[]> OnVideoSourceEncodedSample => _recorderService.OnVideoSourceEncodedSample;
            Action<uint, int, int, byte[], VideoPixelFormatsEnum> OnVideoSourceRawSample => _recorderService.OnVideoSourceRawSample;
            MediaFormatManager<VideoFormat> _formatManager => _recorderService._formatManager;


            public AVCaptureVideoDataDelegate(VideoRecorderService recorderService)
            {
                _recorderService = recorderService;
            }


            private DateTime _lastFrameAt = DateTime.MinValue;

            public override void DidOutputSampleBuffer(AVCaptureOutput output, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
            {
                using (output)
                using (connection)

                using (sampleBuffer)
                {
                    try
                    {
                        using var imageBuffer = sampleBuffer.GetImageBuffer();

                        if (imageBuffer is not CVPixelBuffer pixelBuffer) return;

                        int width = (int)pixelBuffer.Width;
                        int height = (int)pixelBuffer.Height;
                        int bytesPerRow = (int)pixelBuffer.BytesPerRow;

                        pixelBuffer.Lock(CVPixelBufferLock.ReadOnly);

                        try
                        {
                            var bgra = new byte[width * height * 4];

                            var baseAddress = pixelBuffer.BaseAddress;

                            // 如果没有 padding，可以直接复制
                            if (bytesPerRow == width * 4)
                            {
                                Marshal.Copy(baseAddress, bgra, 0, bgra.Length);
                            }
                            else
                            {
                                // 有 stride 时逐行复制
                                for (int y = 0; y < height; y++)
                                {
                                    Marshal.Copy(IntPtr.Add(baseAddress, y * bytesPerRow), bgra, y * width * 4, width * 4);
                                }
                            }

                            // BGRA -> I420
                            var i420 = PixelConverter.BGRAtoI420(bgra, width, height, width * 4);

                            if (OnVideoSourceEncodedSample != null && _videoEncoder != null && !_formatManager.SelectedFormat.IsEmpty())
                            {
                                lock (_videoEncoder)
                                {
                                    var encodedBuffer = _videoEncoder.EncodeVideo(width, height, i420, VideoPixelFormatsEnum.I420, _formatManager.SelectedFormat.Codec);
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

                                OnVideoSourceRawSample.Invoke(frameSpacing, width, height, i420, VideoPixelFormatsEnum.I420);
                            }
                        }
                        finally
                        {
                            pixelBuffer.Unlock(CVPixelBufferLock.ReadOnly);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }

                _lastFrameAt = DateTime.Now;
            }
        }

    }
}
