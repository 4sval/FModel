using System.IO;
using PakReader.Parsers.Objects;

namespace PakReader
{
    public class LocMetaReader
    {
        static readonly FGuid Magic = new FGuid(0xA14CEE4F, 0x83554868, 0xBD464C6C, 0x7C50DA70);
        public readonly string NativeCulture;
        public readonly string NativeLocRes;

        public LocMetaReader(Stream stream) : this(new BinaryReader(stream)) { }

        public LocMetaReader(BinaryReader reader)
        {
            if (Magic != new FGuid(reader))
            {
                throw new IOException("LocMeta file has an invalid magic constant!");
            }

            var VersionNumber = (Version)reader.ReadByte();
            if (VersionNumber > Version.LATEST)
            {
                throw new IOException($"LocMeta file is too new to be loaded! (File Version: {(byte)VersionNumber}, Loader Version: {(byte)Version.LATEST})");
            }

            NativeCulture = reader.ReadFString();
            NativeLocRes = reader.ReadFString();
        }

        public enum Version : byte
        {
            /** Initial format. */
            INITIAL = 0,

            LATEST_PLUS_ONE,
            LATEST = LATEST_PLUS_ONE - 1
        }
    }
}
