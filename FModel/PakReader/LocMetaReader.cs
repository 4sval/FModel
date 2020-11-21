using System.IO;
using FModel.PakReader.Parsers.Objects;

namespace FModel.PakReader
{
    public class LocMetaReader
    {
        static readonly FGuid Magic = new FGuid(0xA14CEE4F, 0x83554868, 0xBD464C6C, 0x7C50DA70);
        public readonly string NativeCulture;
        public readonly string NativeLocRes;
        public readonly string[] CompiledCultures;

        public LocMetaReader(Stream stream) : this(new BinaryReader(stream)) { }

        public LocMetaReader(BinaryReader reader)
        {
            if (Magic != new FGuid(reader))
            {
                throw new IOException("LocMeta file has an invalid magic constant!");
            }

            var VersionNumber = (Version)reader.ReadByte();
            if (VersionNumber > Version.Latest)
            {
                throw new IOException($"LocMeta file is too new to be loaded! (File Version: {(byte)VersionNumber}, Loader Version: {(byte)Version.Latest})");
            }

            NativeCulture = reader.ReadFString();
            NativeLocRes = reader.ReadFString();

            if (VersionNumber >= Version.AddedCompiledCultures)
            {
                CompiledCultures = reader.ReadTArray(() => reader.ReadFString());
            }
        }

        public enum Version : byte
        {
            /** Initial format. */
            Initial = 0,
            /** Added complete list of cultures compiled for the localization target. */
            AddedCompiledCultures,

            LatestPlusOne,
            Latest = LatestPlusOne - 1
        }
    }
}
