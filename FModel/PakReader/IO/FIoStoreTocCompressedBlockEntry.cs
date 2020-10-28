using System.IO;
using System.Runtime.CompilerServices;

namespace FModel.PakReader.IO
{
    public readonly struct FIoStoreTocCompressedBlockEntry
    {
        private const int OffsetBits = 40;
        private const ulong OffsetMask = (1ul << OffsetBits) - 1ul;
        private const int SizeBits = 24;
        private const uint SizeMask = (1 << SizeBits) - 1;
        private const int SizeShift = 8;
        
        
        /* 5 bytes offset, 3 bytes for size / uncompressed size and 1 byte for compresseion method. */
        public readonly byte[] Data;

        public long Offset
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                unsafe { fixed (byte* ptr = Data) {
                        var offset = (ulong*) ptr;
                        return (long) (*offset & OffsetMask); 
                } }
            }
        }
        public uint CompressedSize
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                unsafe { fixed (byte* ptr = Data) {
                    var size = ((uint*) ptr) + 1;
                    return (*size >> SizeShift) & SizeMask; 
                } }
            }
        }
        public uint UncompressedSize
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                unsafe { fixed (byte* ptr = Data) {
                    var uncompressedSize = ((uint*) ptr) + 2;
                    return *uncompressedSize & SizeMask; 
                } }
            }
        }
        
        public byte CompressionMethodIndex
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                unsafe { fixed (byte* ptr = Data) {
                    var index = ((uint*) ptr) + 2;
                    return (byte) (*index >> SizeBits); 
                } }
            }
        }

        public FIoStoreTocCompressedBlockEntry(BinaryReader reader)
        {
            Data = reader.ReadBytes(5 + 3 + 3 + 1);
        }
    }
}