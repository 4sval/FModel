using System.IO;

namespace FModel.PakReader.IO
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