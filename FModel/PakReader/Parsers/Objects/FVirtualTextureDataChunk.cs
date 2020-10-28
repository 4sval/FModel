using System.IO;

namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct FVirtualTextureDataChunk
    {
        public readonly FByteBulkData BulkData;
        public readonly uint SizeInBytes;
        public readonly uint CodecPayloadSize;
        public readonly ushort[] CodecPayloadOffset;
        public readonly EVirtualTextureCodec[] CodecType;

        internal FVirtualTextureDataChunk(BinaryReader reader, Stream ubulk, long bulkOffset, uint numLayers)
        {
            CodecPayloadOffset = new ushort[8];
            CodecType = new EVirtualTextureCodec[8];

            SizeInBytes = reader.ReadUInt32();
            CodecPayloadSize = reader.ReadUInt32();
            for (int LayerIndex = 0; LayerIndex < numLayers; ++LayerIndex)
            {
                byte CodecTypeAsByte = reader.ReadByte();
                CodecType[LayerIndex] = (EVirtualTextureCodec)CodecTypeAsByte;
                CodecPayloadOffset[LayerIndex] = reader.ReadUInt16();
            }

            BulkData = new FByteBulkData(reader, ubulk, bulkOffset);
        }
    }
}
