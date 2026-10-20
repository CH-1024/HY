using SIPSorceryMedia.Abstractions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace HY.MAUI.Interfaces
{
    public interface IAudioRecorderService
    {
        protected static readonly List<AudioFormat> SupportedFormats = new List<AudioFormat>
        {
            new AudioFormat(AudioCodecsEnum.OPUS, 111, 48000, 2),
        };



        bool IsStarting { get; }



        event Action<uint, byte[]> OnAudioSourceEncodedSample;

        void SetAudioEncoder(IAudioEncoder encoder);
        void SetAudioFormat(AudioFormat format);
        AudioFormat GetSelectedFormat();
        List<AudioFormat> GetAudioFormats();

        void GotAudioFrame(EncodedAudioFrame encodedAudio);

        Task<bool> StartRecording();
        Task<bool> StopRecording();
    }
}
