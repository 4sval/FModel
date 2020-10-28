using FModel.PakReader.Parsers;
using FModel.PakReader.Parsers.Objects;

namespace FModel.PakReader.IO
{
    public readonly struct FExportMapEntry
    {
        public const int SIZE = 72;
        
        public readonly ulong CookedSerialOffset;
        public readonly ulong CookedSerialSize;
        public readonly FMappedName ObjectName;
        public readonly FPackageObjectIndex OuterIndex;
        public readonly FPackageObjectIndex ClassIndex;
        public readonly FPackageObjectIndex SuperIndex;
        public readonly FPackageObjectIndex TemplateIndex;
        public readonly FPackageObjectIndex GlobalImportIndex;
        public readonly EObjectFlags ObjectFlags;
        public readonly EExportFilterFlags FilterFlags;

        public FExportMapEntry(IoPackageReader reader)
        {
            CookedSerialOffset = reader.ReadUInt64();
            CookedSerialSize = reader.ReadUInt64();
            ObjectName = new FMappedName(reader);
            OuterIndex = new FPackageObjectIndex(reader);
            ClassIndex = new FPackageObjectIndex(reader);
            SuperIndex = new FPackageObjectIndex(reader);
            TemplateIndex = new FPackageObjectIndex(reader);
            GlobalImportIndex = new FPackageObjectIndex(reader);

            ObjectFlags = (EObjectFlags) reader.ReadUInt32();
            FilterFlags = (EExportFilterFlags) reader.ReadByte();
            reader.SkipBytes(3);
        }
    }
}