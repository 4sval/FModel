using System;
using System.IO;
using System.Runtime.InteropServices;

namespace PakReader
{
    public sealed class UDicFile
    {
        public readonly FDictionaryHeader Header;
        public readonly byte[] DictionaryData;
        public readonly byte[] CompressorState;

        public UDicFile(string path) : this(File.OpenRead(path)) { }

        public UDicFile(Stream stream) : this(new BinaryReader(stream)) { }

        public UDicFile(BinaryReader reader)
        {
            Header = new FDictionaryHeader(reader);
            DictionaryData = DecompressOodle(reader, Header.DictionaryData);
            var CompactCompressorState = DecompressOodle(reader, Header.CompressorData);

            CompressorState = new byte[OodleNetwork1UDP_State_Size()];
            OodleNetwork1UDP_State_Uncompact(CompressorState, CompactCompressorState);
        }

        [DllImport("oo2core_5_win64")]
        private static extern int OodleLZ_Decompress(byte[] buffer, ulong bufferSize, byte[] outputBuffer, ulong outputBufferSize, uint a, uint b, uint c, ulong d, ulong e, ulong f, ulong g, ulong h, ulong i, uint j);
        private static int OodleLZ_Decompress(byte[] buffer, byte[] outputBuffer) => OodleLZ_Decompress(buffer, (ulong)buffer.Length, outputBuffer, (ulong)outputBuffer.Length, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

        [DllImport("oo2core_5_win64")]
        private static extern uint OodleNetwork1UDP_State_Size();

        [DllImport("oo2core_5_win64")]
        private static extern uint OodleNetwork1UDP_State_Uncompact(byte[] state, byte[] compressorState);

        // [DllImport("oo2core_5_win64")]
        // private static extern int OodleLZ_Compress(uint format, byte[] buffer, long bufferSize, byte[] outputBuffer, ulong level);

        static byte[] DecompressOodle(BinaryReader reader, FOodleCompressedData DataInfo)
        {
            reader.BaseStream.Seek(DataInfo.Offset, SeekOrigin.Begin);
            var CompressedData = reader.ReadBytes((int)DataInfo.CompressedLength);
            var DecompressedData = new byte[DataInfo.DecompressedLength];

            var OodleLen = OodleLZ_Decompress(CompressedData, DecompressedData);
            if (OodleLen != DataInfo.DecompressedLength)
            {
                Console.WriteLine($"Didn't decompress correctly. Read {OodleLen}, should be {DataInfo.DecompressedLength}");
            }
            return DecompressedData;
        }
    }

    public class FDictionaryHeader
    {
        public uint Magic;
        public uint DictionaryVersion;
        public uint OodleMajorHeaderVersion;
        public int HashTableSize;
        public FOodleCompressedData DictionaryData;
        public FOodleCompressedData CompressorData;

        public FDictionaryHeader(BinaryReader reader)
        {
            Magic = reader.ReadUInt32();
            DictionaryVersion = reader.ReadUInt32();
            OodleMajorHeaderVersion = reader.ReadUInt32();
            HashTableSize = reader.ReadInt32();
            DictionaryData = new FOodleCompressedData(reader);
            CompressorData = new FOodleCompressedData(reader);
        }
    }

    public class FOodleCompressedData
    {
        public uint Offset;
        public uint CompressedLength;
        public uint DecompressedLength;

        public FOodleCompressedData(BinaryReader reader)
        {
            Offset = reader.ReadUInt32();
            CompressedLength = reader.ReadUInt32();
            DecompressedLength = reader.ReadUInt32();
        }
    }
}
