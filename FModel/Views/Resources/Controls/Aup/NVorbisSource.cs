using System;
using System.IO;
using CSCore;
using NVorbis;

namespace FModel.Views.Resources.Controls.Aup
{
    public sealed class NVorbisSource : ISampleSource
    {
        private readonly Stream _stream;
        private readonly VorbisReader _vorbisReader;
        private bool _disposed;

        public WaveFormat WaveFormat { get; }

        public NVorbisSource(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead)
                throw new ArgumentException(@"Stream is not readable.", nameof(stream));

            _stream = stream;
            _vorbisReader = new VorbisReader(stream, false);
            WaveFormat = new WaveFormat(_vorbisReader.SampleRate, 32, _vorbisReader.Channels, AudioEncoding.IeeeFloat);
        }

        public bool CanSeek => _stream.CanSeek;
        public long Length => CanSeek ? (long) (_vorbisReader.TotalTime.TotalSeconds * WaveFormat.SampleRate * WaveFormat.Channels) : 0;

        public long Position
        {
            get => CanSeek ? (long) (_vorbisReader.TimePosition.TotalSeconds * _vorbisReader.SampleRate * _vorbisReader.Channels) : 0;
            set
            {
                if (!CanSeek)
                    throw new InvalidOperationException("NVorbisSource is not seekable.");
                if (value < 0 || value > Length)
                    throw new ArgumentOutOfRangeException(nameof(value));

                _vorbisReader.TimePosition = TimeSpan.FromSeconds((double) value / _vorbisReader.SampleRate / _vorbisReader.Channels);
            }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            return _vorbisReader.ReadSamples(buffer, offset, count);
        }

        public void Dispose()
        {
            if (!_disposed)
                _vorbisReader.Dispose();
            else
                throw new ObjectDisposedException("NVorbisSource");

            _disposed = true;
        }
    }
}