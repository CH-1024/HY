using HY.MAUI.Services.Interfaces;
using SIPSorceryMedia.Abstractions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;
using Windows.Foundation;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Media.Render;
using WinRT;

namespace HY.MAUI.Platforms.Windows
{
    public class AudioRecorderService : IAudioRecorderService
    {
        private AudioGraph _audioGraph;

        private AudioDeviceInputNode _deviceInputNode;
        private AudioFrameOutputNode _frameOutputNode;

        private AudioFrameInputNode _frameInputNode;
        private AudioDeviceOutputNode _deviceOutputNode;


        CancellationTokenSource _cts;
        IAudioEncoder _audioEncoder;
        MediaFormatManager<AudioFormat> _formatManager;
        Channel<short[]> _audioInputQueue;
        Channel<byte[]> _audioOutputQueue;

        public bool IsStarting { get; private set; }

        public event Action<uint, byte[]> OnAudioSourceEncodedSample;



        public void SetAudioEncoder(IAudioEncoder encoder)
        {
            _audioEncoder = encoder;
            _formatManager = new MediaFormatManager<AudioFormat>(IAudioRecorderService.SupportedFormats);

            // 默认OPUS
            _formatManager.SetSelectedFormat(IAudioRecorderService.SupportedFormats.Find(x => x.Codec == AudioCodecsEnum.OPUS));
        }

        public void RestrictFormats(Func<AudioFormat, bool> filter)
        {
            _formatManager.RestrictFormats(filter);
        }

        public void SetAudioFormat(AudioFormat format)
        {
            _formatManager.SetSelectedFormat(format);
        }

        public AudioFormat GetSelectedFormat()
        {
            return _formatManager.SelectedFormat;
        }

        public List<AudioFormat> GetAudioFormats()
        {
            return _formatManager.GetSourceFormats();
        }


        public void GotAudioFrame(EncodedAudioFrame encodedAudioFrame)
        {
            if (!IsStarting) return;

            var audioFormat = encodedAudioFrame.AudioFormat;

            if (_audioEncoder != null && !audioFormat.IsEmpty())
            {
                var pcmSample = _audioEncoder.DecodeAudio(encodedAudioFrame.EncodedAudio, audioFormat);
                byte[] pcmBytes = pcmSample.SelectMany(BitConverter.GetBytes).ToArray();

                _audioOutputQueue.Writer.TryWrite(pcmBytes);
            }
        }




        //public string GetAudioInfo()
        //{
        //    // 使用 AudioGraph 获取音频设备信息
        //    var audioGraphSettings = new AudioGraphSettings(AudioRenderCategory.Communications);
        //    var audioGraphResult = AudioGraph.CreateAsync(audioGraphSettings).GetAwaiter().GetResult();

        //    if (audioGraphResult.Status != AudioGraphCreationStatus.Success)
        //    {
        //        return "无法获取音频设备信息。";
        //    }

        //    var audioGraph = audioGraphResult.Graph;
        //    return $"Windows 音频设备: {audioGraphSettings.PrimaryRenderDevice.Id}";
        //}


        public async Task<bool> StartRecording()
        {
            if (IsStarting) return false;

            var pcmEncoding = AudioEncodingProperties.CreatePcm(
                48000, // 48kHz
                1,     // 单声道
                16);   // 16-bit PCM

            var graphSettings = new AudioGraphSettings(AudioRenderCategory.Communications)
            {
                EncodingProperties = pcmEncoding,
                DesiredRenderDeviceAudioProcessing = AudioProcessing.Default,
                QuantumSizeSelectionMode = QuantumSizeSelectionMode.LowestLatency,
                DesiredSamplesPerQuantum = 480
            };

            // 初始化音频图
            _audioGraph = (await AudioGraph.CreateAsync(graphSettings)).Graph;

            // 录音
            _frameOutputNode = _audioGraph.CreateFrameOutputNode(pcmEncoding);
            _deviceInputNode = (await _audioGraph.CreateDeviceInputNodeAsync(MediaCategory.Communications, pcmEncoding)).DeviceInputNode;
            _deviceInputNode.AddOutgoingConnection(_frameOutputNode);
            _deviceInputNode.OutgoingGain = 0.5f;
            _audioGraph.QuantumStarted += OnQuantumStarted;

            // 放音
            _deviceOutputNode = (await _audioGraph.CreateDeviceOutputNodeAsync()).DeviceOutputNode;
            _frameInputNode = _audioGraph.CreateFrameInputNode(pcmEncoding);
            _frameInputNode.AddOutgoingConnection(_deviceOutputNode);
            _frameInputNode.OutgoingGain = 0.5f;
            _audioGraph.QuantumProcessed += OnQuantumProcessed;

            _audioInputQueue = Channel.CreateBounded<short[]>(new BoundedChannelOptions(8)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.DropOldest
            });
            _audioOutputQueue = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(8)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.DropOldest
            });

            _cts = new CancellationTokenSource();
            _ = Task.Run(async () => await ProcessAudioAsync(_cts.Token));

            _audioGraph.Start();

            IsStarting = true;

            return true;
        }


        public async Task<bool> StopRecording()
        {
            if (!IsStarting) return false;

            IsStarting = false;

            _audioGraph?.Stop();

            await _cts.CancelAsync();

            _deviceInputNode?.Dispose();
            _frameOutputNode?.Dispose();

            _frameInputNode?.Dispose();
            _deviceOutputNode?.Dispose();

            _audioGraph?.Dispose();

            _audioGraph = null;
            _deviceInputNode = null;
            _frameOutputNode = null;
            _frameInputNode = null;
            _deviceOutputNode = null;

            _audioInputQueue = null;
            _audioOutputQueue = null;

            return true;
        }







        private async Task ProcessAudioAsync(CancellationToken cancellationToken)
        {
            short[] frame = new short[960];
            int offset = 0;

            await foreach (var chunk in _audioInputQueue.Reader.ReadAllAsync(cancellationToken))
            {
                int sourceOffset = 0;

                while (sourceOffset < chunk.Length)
                {
                    int count = Math.Min(frame.Length - offset, chunk.Length - sourceOffset);

                    Array.Copy(chunk, sourceOffset, frame, offset, count);

                    offset += count;
                    sourceOffset += count;

                    if (offset == frame.Length)
                    {
                        if (OnAudioSourceEncodedSample != null && _audioEncoder != null && !_formatManager.SelectedFormat.IsEmpty())
                        {
                            lock (_audioEncoder)
                            {
                                var encodedBuffer = _audioEncoder.EncodeAudio(frame, _formatManager.SelectedFormat);
                                if (encodedBuffer != null)
                                {
                                    OnAudioSourceEncodedSample?.Invoke(960, encodedBuffer);
                                }
                            }
                        }

                        frame = new short[960];
                        offset = 0;
                    }
                }
            }
        }

        private unsafe void OnQuantumStarted(AudioGraph sender, object args)
        {
            if (!IsStarting || _frameOutputNode == null || _audioInputQueue == null) return;

            using var frame = _frameOutputNode.GetFrame();

            if (frame.Duration == null || frame.Duration.Value <= TimeSpan.Zero) return;

            using var buffer = frame.LockBuffer(AudioBufferAccessMode.Read);
            using var reference = buffer.CreateReference();

            reference.As<IMemoryBufferByteAccess>().GetBuffer(out var dataPtr, out var capacity);

            if (dataPtr == null || capacity < sizeof(float)) return;

            int sampleCount = checked((int)(capacity / sizeof(float)));

            var floatBuffer = (float*)dataPtr;
            var pcmSamples = new short[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float sample = floatBuffer[i];

                if (!float.IsFinite(sample))
                {
                    sample = 0;
                }

                sample = Math.Clamp(sample, -1f, 1f);

                pcmSamples[i] = (short)Math.Clamp(
                    (int)Math.Round(sample * 32768f),
                    short.MinValue,
                    short.MaxValue);
            }

            _audioInputQueue.Writer.TryWrite(pcmSamples);
        }



        private unsafe void OnQuantumProcessed(AudioGraph sender, object args)
        {
            if (!IsStarting) return;

            if (_frameInputNode == null || !_audioOutputQueue.Reader.TryRead(out var pcmBytes)) return;

            try
            {
                AudioFrame frame = new AudioFrame((uint)pcmBytes.Length);

                using (AudioBuffer buffer = frame.LockBuffer(AudioBufferAccessMode.Write))
                using (IMemoryBufferReference reference = buffer.CreateReference())
                {
                    // 获取缓冲区指针并复制数据
                    reference.As<IMemoryBufferByteAccess>().GetBuffer(out var dataPtr, out var capacity);
                    Marshal.Copy(pcmBytes, 0, (nint)dataPtr, pcmBytes.Length);
                }

                _frameInputNode.AddFrame(frame);
            }
            catch (Exception e)
            {
                ;
            }
        }

    }
}
