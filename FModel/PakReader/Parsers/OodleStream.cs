using System;
using System.IO;
using System.Runtime.InteropServices;
using FModel.Utils;

namespace PakReader.Parsers
{
    /// <summary>
    /// https://gist.github.com/Scobalula/37229307de57de685d16ec621d5aceb5
    /// </summary>
    public class OodleStream : Stream
    {
        protected internal Stream _baseStream;
        bool _disposed;

        public OodleStream(byte[] input, long decompressedLength)
        {
            Oodle.LoadOodleDll();
            _baseStream = new MemoryStream(Decompress(input, decompressedLength), false)
            {
                Position = 0
            };
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!_disposed)
                {
                    if (disposing && (this._baseStream != null))
                        this._baseStream.Dispose();
                    _disposed = true;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        /// <summary>
        /// Oodle64 Decompression Method 
        /// </summary>
        [DllImport(Oodle.OODLE_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern long OodleLZ_Decompress(byte[] buffer, long bufferSize, byte[] result, long outputBufferSize, int a, int b, int c, long d, long e, long f, long g, long h, long i, int ThreadModule);

        /// <summary>
        /// Decompresses a byte array of Oodle Compressed Data (Requires Oodle DLL)
        /// </summary>
        /// <param name="input">Input Compressed Data</param>
        /// <param name="decompressedLength">Decompressed Size</param>
        /// <returns>Resulting Array if success, otherwise null.</returns>
        public static byte[] Decompress(byte[] input, long decompressedLength)
        {
            // Resulting decompressed Data
            byte[] result = new byte[decompressedLength];
            // Decode the data (other parameters such as callbacks not required)
            long decodedSize = OodleLZ_Decompress(input, input.Length, result, decompressedLength, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3);
            // Check did we fail
            if (decodedSize == 0) return null;
            // Return Result
            return result;
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_disposed) throw new ObjectDisposedException("OodleStream");
            return _baseStream.Read(buffer, offset, count);
        }

        public override void Flush() => throw new NotImplementedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
        public override void SetLength(long value) => throw new NotImplementedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
        public override bool CanRead => throw new NotImplementedException();
        public override bool CanSeek => throw new NotImplementedException();
        public override bool CanWrite => throw new NotImplementedException();
        public override long Length => _baseStream.Length;
        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}
