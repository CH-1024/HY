using AudioToolbox;
using AVFoundation;
using HY.MAUI.Interfaces;
using SIPSorceryMedia.Abstractions;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;
using AudioFormat = SIPSorceryMedia.Abstractions.AudioFormat;

namespace HY.MAUI.Platforms.MacCatalyst
{
    public class AudioRecorderService : IAudioRecorderService
    {
        private AVAudioSession _avAudioSession;
        private InputAudioQueue _inputAudioQueue;
        private OutputAudioQueue _outputAudioQueue;


        CancellationTokenSource _cts;
        Task _processAudioTask;
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





        public async Task<bool> StartRecording()
        {
            if (IsStarting) return false;

            var audioFormat = AudioStreamBasicDescription.CreateLinearPCM(
                48000, // 48kHz
                1,     // 单声道
                16);   // 16-bit PCM

            _inputAudioQueue = new InputAudioQueue(audioFormat);
            _inputAudioQueue.InputCompleted += OnInputCompleted;
            _outputAudioQueue = new OutputAudioQueue(audioFormat);
            _outputAudioQueue.BufferCompleted += OnBufferCompleted;

            const int bufferSize = 1920;
            for (int i = 0; i < 3; i++)
            {
                unsafe
                {
                    _inputAudioQueue.AllocateBuffer(bufferSize, out AudioQueueBuffer* inputBuffer);
                    _inputAudioQueue.EnqueueBuffer((nint)inputBuffer, bufferSize, null);

                    _outputAudioQueue.AllocateBuffer(bufferSize, out AudioQueueBuffer* outputBuffer);
                    _outputAudioQueue.EnqueueBuffer((nint)outputBuffer, bufferSize, null);
                }
            }

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
            _processAudioTask = Task.Run(async () => await ProcessAudioAsync(_cts.Token));

            _inputAudioQueue.Start();
            _outputAudioQueue.Start();

            IsStarting = true;

            return true;
        }


        public async Task<bool> StopRecording()
        {
            if (!IsStarting) return false;

            IsStarting = false;

            _inputAudioQueue?.InputCompleted -= OnInputCompleted;
            _outputAudioQueue?.BufferCompleted -= OnBufferCompleted;
            _inputAudioQueue?.Stop(true);
            _outputAudioQueue?.Stop(true);

            await _cts.CancelAsync();
            await _processAudioTask;

            _inputAudioQueue?.Dispose();
            _outputAudioQueue?.Dispose();

            _cts = null;
            _processAudioTask = null;

            _inputAudioQueue = null;
            _outputAudioQueue = null;

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

        private void OnInputCompleted(object? sender, InputCompletedEventArgs e)
        {
            if (!IsStarting) return;

            try
            {
                int byteCount = (int)e.Buffer.AudioDataByteSize;

                if (byteCount <= 0) return;

                // 16-bit PCM，每个采样占 2 字节
                int sampleCount = byteCount / sizeof(short);

                if (sampleCount <= 0) return;

                // 防止异常的奇数字节数
                byteCount = sampleCount * sizeof(short);

                byte[] pcmBytes = new byte[byteCount];
                Marshal.Copy(e.Buffer.AudioData, pcmBytes, 0, byteCount);

                short[] pcmSamples = new short[sampleCount];

                Buffer.BlockCopy(pcmBytes, 0, pcmSamples, 0, byteCount);

                _audioInputQueue.Writer.TryWrite(pcmSamples);
            }
            finally
            {
                _inputAudioQueue?.EnqueueBuffer(e.IntPtrBuffer, null);
            }
        }

        private unsafe void OnBufferCompleted(object? sender, BufferCompletedEventArgs e)
        {
            if (!IsStarting || _outputAudioQueue == null) return;

            var buffer = e.UnsafeBuffer;
            int capacity = (int)buffer->AudioDataBytesCapacity;

            try
            {
                if (_audioOutputQueue.Reader.TryRead(out var pcmBytes) && pcmBytes.Length > 0)
                {
                    int byteCount = pcmBytes.Length;

                    if (byteCount <= capacity)
                    {
                        Marshal.Copy(pcmBytes, 0, buffer->AudioData, byteCount);

                        _outputAudioQueue?.EnqueueBuffer(e.IntPtrBuffer, byteCount, null);

                        return;
                    }
                }

                // 无数据时清空缓冲区，避免播放旧数据。
                if (capacity > 0) new Span<byte>((void*)buffer->AudioData, capacity).Clear();

                _outputAudioQueue?.EnqueueBuffer(e.IntPtrBuffer, capacity, null);
            }
            catch (Exception ex)
            {
                //System.Diagnostics.Debug.WriteLine($"音频播放缓冲区处理失败：{ex}");
            }
        }

    }
}
