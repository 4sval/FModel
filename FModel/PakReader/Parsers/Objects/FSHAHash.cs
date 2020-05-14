using System.IO;

namespace PakReader.Parsers.Objects
{
    public readonly struct FSHAHash
    {
        public readonly byte[] Hash;

        internal FSHAHash(BinaryReader reader)
        {
            Hash = reader.ReadBytes(20);
        }
    }
}
