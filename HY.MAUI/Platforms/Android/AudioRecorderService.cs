using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;
using Android.Media;
using HY.MAUI.Interfaces;
using SIPSorceryMedia.Abstractions;
using AudioFormat = SIPSorceryMedia.Abstractions.AudioFormat;
using Encoding = Android.Media.Encoding;

namespace HY.MAUI.Platforms.Android;

public class AudioRecorderService : IAudioRecorderService
    {
        private AudioRecord _audioRecord;
        private AudioTrack _audioTrack;
        private Task _captureAudioTask;
        private Task _playAudioTask;

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
            
            var sampleRate = 48000;
            var channelInConfig = ChannelIn.Mono;
            var channelOutConfig = ChannelOut.Mono;
            var encoding = Encoding.Pcm16bit;

            int inMinBufferSize = AudioRecord.GetMinBufferSize(sampleRate, channelInConfig, encoding);
            _audioRecord = new AudioRecord(AudioSource.VoiceCommunication, sampleRate, channelInConfig, encoding, inMinBufferSize);

            var outMinBufferSize = AudioTrack.GetMinBufferSize(sampleRate, channelOutConfig, encoding);
            var format = new global:: Android.Media.AudioFormat.Builder().SetSampleRate(sampleRate)?.SetChannelMask(channelOutConfig)?.SetEncoding(encoding)?.Build();
            var attributes = new AudioAttributes.Builder().SetUsage(AudioUsageKind.VoiceCommunication)?.SetContentType(AudioContentType.Sonification)?.Build();
            _audioTrack = new AudioTrack(attributes, format, outMinBufferSize, AudioTrackMode.Stream, AudioManager.AudioSessionIdGenerate);
            // _audioTrack = new AudioTrack(global::Android.Media.Stream.VoiceCall, sampleRate, channelOutConfig, encoding, outMinBufferSize, AudioTrackMode.Stream);
            
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
            _captureAudioTask = Task.Run(async () => await OnCapturingAsync(inMinBufferSize, _cts.Token));
            _playAudioTask = Task.Run(async () => await OnPlayingAsync(_cts.Token));
            
            _audioRecord.StartRecording();
            _audioTrack.Play();

            IsStarting = true;

            return true;
        }


        public async Task<bool> StopRecording()
        {
            if (!IsStarting) return false;

            IsStarting = false;
            
            _audioRecord?.Stop();
            _audioTrack.Stop();
            
            await _cts.CancelAsync();
            await _processAudioTask;
            await _captureAudioTask;
            await _playAudioTask;
            
            _audioRecord?.Release();
            _audioTrack?.Release();
            
            _cts = null;
            _processAudioTask = null;
            _captureAudioTask = null;
            _playAudioTask = null;

            _audioRecord =  null;
            _audioTrack = null;
            
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

        private async Task OnCapturingAsync(int bufferSize, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[bufferSize];

            while (!cancellationToken.IsCancellationRequested)
            {
                int bytesRead = await _audioRecord.ReadAsync(buffer, 0, buffer.Length);

                if (bytesRead <= 0) continue;
                
                // PCM 16-bit：每个采样点占 2 字节
                int sampleCount = bytesRead / sizeof(short);
                short[] pcmSamples = new short[sampleCount];

                Buffer.BlockCopy(buffer, 0, pcmSamples, 0, sampleCount * sizeof(short));

                _audioInputQueue.Writer.TryWrite(pcmSamples);
            }
        }
        
        private async Task OnPlayingAsync(CancellationToken cancellationToken)
        {
            await foreach (var pcmData in _audioOutputQueue.Reader.ReadAllAsync(cancellationToken))
            {
                await _audioTrack.WriteAsync(pcmData, 0, pcmData.Length);
            }
        }
        
    }