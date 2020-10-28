using FModel.PakReader.Parsers.Objects;

namespace FModel.PakReader.IO
{
    public class FPackageDesc
    {
        public FPackageId PackageId;
        public FName PackageName;
        public ulong Size = 0;
        public uint LoadOrder = uint.MaxValue;
        public uint PackageFlags = 0;
        public int NameCount = -1;
        public int ExportBundleCount = -1;
        public FPackageLocation[] Locations;
        public FImportDesc[] Imports;
        public FExportDesc[] Exports;
    }
}