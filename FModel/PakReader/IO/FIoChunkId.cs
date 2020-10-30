using System;
using System.IO;
using System.Linq;

namespace FModel.PakReader.IO
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

        public FIoChunkId(ulong chunkId, ushort chunkIndex, EIoChunkType ioChunkType)
        {
            Id = new byte[12];
            Buffer.BlockCopy(BitConverter.GetBytes(chunkId), 0, Id, 0, sizeof(ulong));
            Buffer.BlockCopy(BitConverter.GetBytes(chunkIndex), 0, Id, sizeof(ulong), sizeof(ushort));
            Id[11] = (byte)ioChunkType;
        }

        public override int GetHashCode()
        {
            var hash = 5381;
            for (var i = 0; i < 12; i++)
            {
                hash = hash * 33 + Id[i];
            }

            return hash;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is FIoChunkId cast))
            {
                return false;
            }

            return Id.SequenceEqual(cast.Id);
        }

        public static bool operator ==(FIoChunkId a, FIoChunkId b)
        {
            return a.Id.SequenceEqual(b.Id);
        }

        public static bool operator !=(FIoChunkId a, FIoChunkId b)
        {
            return !a.Id.SequenceEqual(b.Id);
        }

        public override string ToString()
        {
            return BitConverter.ToString(Id).Replace("-", "");
        }
    }
}