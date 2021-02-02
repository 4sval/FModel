using System.IO;

namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct FEngineVersion
    {
        // FEngineVersionBase
        public readonly ushort Major;
        public readonly ushort Minor;
        public readonly ushort Patch;
        public readonly uint Changelist;

        // FEngineVersion
        public readonly string Branch;

        internal FEngineVersion(BinaryReader reader)
        {
            Major = reader.ReadUInt16();
            Minor = reader.ReadUInt16();
            Patch = reader.ReadUInt16();
            Changelist = reader.ReadUInt32();
            Branch = reader.ReadFString();
        }

        internal FEngineVersion(ushort major, ushort minor, ushort patch, uint changelist, string branch)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            Changelist = changelist;
            Branch = branch;
        }
    }
}
