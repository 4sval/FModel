using System.IO;

namespace PakReader.Parsers.Objects
{
    public readonly struct FTexture2DMipMap
    {
        public readonly int SizeX;
        public readonly int SizeY;
        public readonly int SizeZ;
        public readonly FByteBulkData BulkData;

        internal FTexture2DMipMap(BinaryReader reader, Stream ubulk, long bulkOffset)
        {
            var bCooked = reader.ReadInt32() != 0;
            BulkData = new FByteBulkData(reader, ubulk, bulkOffset);
            SizeX = reader.ReadInt32();
            SizeY = reader.ReadInt32();
            SizeZ = reader.ReadInt32();
        }
    }
}
