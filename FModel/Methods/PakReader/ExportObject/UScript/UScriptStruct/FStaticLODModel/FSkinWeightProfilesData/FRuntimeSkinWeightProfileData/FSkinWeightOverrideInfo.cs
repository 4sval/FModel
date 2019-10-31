using System.IO;

namespace PakReader
{
    public struct FSkinWeightOverrideInfo
    {
        public uint influences_offset;
        public byte num_influences;

        public FSkinWeightOverrideInfo(BinaryReader reader)
        {
            influences_offset = reader.ReadUInt32();
            num_influences = reader.ReadByte();
        }
    }
}
