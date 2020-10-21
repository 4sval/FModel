using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PakReader.Pak.IO
{
    public readonly struct FIoChunkId
    {
        public readonly byte[] Id;

        public ulong ChunkId => BitConverter.ToUInt64(Id);
        public ushort ChunkIndex => BitConverter.ToUInt16(Id, 8);
        public EIoChunkType ChunkType => (EIoChunkType) Id[11];

        public FIoChunkId(BinaryReader reader)
        {
            Id = reader.ReadBytes(12);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            var hash = 5381;
            for (int i = 0; i < 12; i++)
            {
                hash = hash * 33 + Id[i];
            }

            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object? obj)
        {
            if (!(obj is FIoChunkId cast)) return false;
            return Id.SequenceEqual(cast.Id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(FIoChunkId a, FIoChunkId b) => a.Id.SequenceEqual(b.Id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(FIoChunkId a, FIoChunkId b) => !a.Id.SequenceEqual(b.Id);
        
        public override string ToString() => BitConverter.ToString(Id).Replace("-","");
    }
}