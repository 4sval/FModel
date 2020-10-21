using System.IO;

namespace PakReader.Pak.IO
{
    public readonly struct FIoDirectoryIndexEntry
    {
        public readonly uint Name;
        public readonly uint FirstChildEntry;
        public readonly uint NextSiblingEntry;
        public readonly uint FirstFileEntry;

        public FIoDirectoryIndexEntry(BinaryReader reader)
        {
            Name = reader.ReadUInt32();
            FirstChildEntry = reader.ReadUInt32();
            NextSiblingEntry = reader.ReadUInt32();
            FirstFileEntry = reader.ReadUInt32();
        }
    }
}