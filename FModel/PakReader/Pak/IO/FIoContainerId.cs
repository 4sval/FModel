using System.IO;

namespace PakReader.Pak.IO
{
    public struct FIoContainerId
    {
        public ulong Id;

        public FIoContainerId(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
        }
    }
}