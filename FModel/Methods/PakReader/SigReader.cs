using System.IO;

namespace PakReader
{
    public sealed class SigFile
    {
        const uint Magic = 0x73832DAA;

        public readonly ESigVersion Version;
        public readonly byte[] EncryptedHash;
        public readonly uint[] ChunkHashes;

        public SigFile(string path) : this(File.OpenRead(path)) { }

        public SigFile(Stream stream) : this(new BinaryReader(stream)) { }

        public SigFile(BinaryReader reader)
        {
            if (reader.ReadUInt32() != Magic)
            {
                reader.BaseStream.Seek(-sizeof(uint), SeekOrigin.Current);
                Version = ESigVersion.First;
                reader.BaseStream.Seek(512 / 8, SeekOrigin.Current); // reads a 512-bit integer, but it isn't used
                ChunkHashes = reader.ReadTArray(() => reader.ReadUInt32());
            }

            Version = (ESigVersion)reader.ReadInt32();
            EncryptedHash = reader.ReadBytes(reader.ReadInt32());
            ChunkHashes = reader.ReadTArray(() => reader.ReadUInt32());
        }
    }

    public enum ESigVersion
    {
        Legacy,
		First,

		Last,
		Latest = Last - 1
	};
}
