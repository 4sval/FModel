using FModel.PakReader.Parsers;

namespace FModel.PakReader.IO
{
    public class FPackageSummary
    {
        public readonly FMappedName Name;
        public readonly FMappedName SourceName;
        public readonly uint PackageFlags;
        public readonly uint CookedHeaderSize;
        public readonly int NameMapNamesOffset;
        public readonly int NameMapNamesSize;
        public readonly int NameMapHashesOffset;
        public readonly int NameMapHashesSize;
        public readonly int ImportMapOffset;
        public readonly int ExportMapOffset;
        public readonly int ExportBundlesOffset;
        public readonly int GraphDataOffset;
        public readonly int GraphDataSize;

        public FPackageSummary(IoPackageReader reader)
        {
            Name = new FMappedName(reader);
            SourceName = new FMappedName(reader);
            PackageFlags = reader.ReadUInt32();
            CookedHeaderSize = reader.ReadUInt32();
            NameMapNamesOffset = reader.ReadInt32();
            NameMapNamesSize = reader.ReadInt32();
            NameMapHashesOffset = reader.ReadInt32();
            NameMapHashesSize = reader.ReadInt32();
            ImportMapOffset = reader.ReadInt32();
            ExportMapOffset = reader.ReadInt32();
            ExportBundlesOffset = reader.ReadInt32();
            GraphDataOffset = reader.ReadInt32();
            GraphDataSize = reader.ReadInt32();
            reader.SkipBytes(4); // Padding
        }
    }
}