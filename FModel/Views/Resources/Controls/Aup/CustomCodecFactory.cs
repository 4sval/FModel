using System;
using System.Collections.Generic;
using System.IO;
using CSCore;
using CSCore.Codecs;
using CSCore.Codecs.AAC;
using CSCore.Codecs.AIFF;
using CSCore.Codecs.DDP;
using CSCore.Codecs.FLAC;
using CSCore.Codecs.MP1;
using CSCore.Codecs.MP2;
using CSCore.Codecs.MP3;
using CSCore.Codecs.WAV;
using CSCore.Codecs.WMA;
using CSCore.MediaFoundation;

namespace FModel.Views.Resources.Controls.Aup
{
    public class CustomCodecFactory
    {
        private readonly Dictionary<string, CodecFactoryEntry> _codecs;

        /// <summary>
        /// TODO add MSADPCM, OPUS, WEM decoders
        /// </summary>
        public CustomCodecFactory()
        {
            _codecs = new Dictionary<string, CodecFactoryEntry>
            {
                ["mp3"] = new(s =>
                {
                    try
                    {
                        return new DmoMp3Decoder(s);
                    }
                    catch (Exception)
                    {
                        if (Mp3MediafoundationDecoder.IsSupported)
                            return new Mp3MediafoundationDecoder(s);
                        throw;
                    }
                }, "mp3", "mpeg3"),
                ["wav"] = new(s =>
                {
                    IWaveSource res = new WaveFileReader(s);
                    if (res.WaveFormat.WaveFormatTag is AudioEncoding.Pcm or AudioEncoding.IeeeFloat or AudioEncoding.Extensible) return res;
                    res.Dispose();
                    res = new MediaFoundationDecoder(s);

                    return res;
                }, "wav", "wave"),
                ["ogg"] = new(s => new NVorbisSource(s).ToWaveSource(), "ogg"),
                ["flac"] = new(s => new FlacFile(s), "flac", "fla"),
                ["aiff"] = new(s => new AiffReader(s), "aiff", "aif", "aifc")
            };

            if (AacDecoder.IsSupported)
            {
                _codecs["aac"] = new CodecFactoryEntry(s => new AacDecoder(s),
                    "aac", "adt", "adts", "m2ts", "mp2", "3g2", "3gp2", "3gp", "3gpp", "m4a", "m4v", "mp4v", "mp4", "mov");
            }

            if (WmaDecoder.IsSupported)
            {
                _codecs["wma"] = new CodecFactoryEntry(s => new WmaDecoder(s), "asf", "wm", "wmv", "wma");
            }

            if (Mp1Decoder.IsSupported)
            {
                _codecs["mp1"] = new CodecFactoryEntry(s => new Mp1Decoder(s), "mp1", "m2ts");
            }

            if (Mp2Decoder.IsSupported)
            {
                _codecs["mp2"] = new CodecFactoryEntry(s => new Mp2Decoder(s), "mp2", "m2ts");
            }

            if (DDPDecoder.IsSupported)
            {
                _codecs["ddp"] = new CodecFactoryEntry(s => new DDPDecoder(s),
                    "mp2", "m2ts", "m4a", "m4v", "mp4v", "mp4", "mov", "asf", "wm", "wmv", "wma", "avi", "ac3", "ec3");
            }
        }

        public IWaveSource GetCodec(byte[] data, string ext)
        {
            var stream = new MemoryStream(data) {Position = 0};
            return _codecs.TryGetValue(ext, out var codecFactoryEntry) ? codecFactoryEntry.GetCodecAction(stream) : new MediaFoundationDecoder(stream);
        }
    }
}