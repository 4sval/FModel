using System.IO;
using static PakReader.AssetReader;

namespace PakReader
{
    public struct FTexturePlatformData
    {
        public int size_x;
        public int size_y;
        public int num_slices;
        public string pixel_format;
        public int first_mip;
        public bool is_virtual;
        public FTexture2DMipMap[] mips;

        internal FTexturePlatformData(BinaryReader reader, BinaryReader ubulk, long bulk_offset)
        {
            size_x = reader.ReadInt32();
            size_y = reader.ReadInt32();
            num_slices = reader.ReadInt32();
            pixel_format = read_string(reader);
            first_mip = reader.ReadInt32();
            mips = new FTexture2DMipMap[reader.ReadUInt32()];
            for (int i = 0; i < mips.Length; i++)
            {
                mips[i] = new FTexture2DMipMap(reader, ubulk, bulk_offset);
            }
            is_virtual = reader.ReadInt32() == 1;
            if (is_virtual)
            {
                throw new IOException("Texture is virtual, unsupported for now");
            }
        }
    }
}
