using System.IO;

namespace PakReader
{
    public struct FMultisizeIndexContainer
    {
        public ushort[] Indices16;
        public uint[] Indices32;

        public FMultisizeIndexContainer(BinaryReader reader)
        {
            var data_size = reader.ReadByte();
            var _element_size = reader.ReadInt32();
            switch (data_size)
            {
                case 2:
                    Indices16 = reader.ReadTArray(() => reader.ReadUInt16());
                    Indices32 = null;
                    return;
                case 4:
                    Indices32 = reader.ReadTArray(() => reader.ReadUInt32());
                    Indices16 = null;
                    return;
                default:
                    throw new FileLoadException("No format size");
            }
        }
    }
}
