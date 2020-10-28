using System.IO;

namespace FModel.PakReader.IO
{
    public readonly struct FSerializedNameHeader
    {
        private readonly byte[] _data;

        public bool IsUtf16 => (_data[0] & 0x80u) != 0;
        public uint Length => ((_data[0] & 0x7Fu) << 8) + _data[1];

        public FSerializedNameHeader(BinaryReader reader)
        {
            _data = reader.ReadBytes(2);
        }
    }
}