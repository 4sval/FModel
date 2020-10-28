using System.IO;

namespace FModel.PakReader.IO
{
    public readonly struct FIoFileIndexEntry
    {
        public readonly uint Name;
        public readonly uint NextFileEntry;
        public readonly uint UserData;

        public FIoFileIndexEntry(BinaryReader reader)
        {
            Name = reader.ReadUInt32();
            NextFileEntry = reader.ReadUInt32();
            UserData = reader.ReadUInt32();
        }
    }
}