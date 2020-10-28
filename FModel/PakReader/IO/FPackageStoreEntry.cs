using System.IO;

namespace FModel.PakReader.IO
{
    public readonly struct FPackageStoreEntry
    {
        public readonly ulong ExportBundlesSize;
        public readonly int ExportCount;
        public readonly int ExportBundleCount;
        public readonly uint LoadOrder;
        public readonly FPackageId[] ImportedPackages;


        public FPackageStoreEntry(BinaryReader reader)
        {
            ExportBundlesSize = reader.ReadUInt64();
            ExportCount = reader.ReadInt32();
            ExportBundleCount = reader.ReadInt32();
            LoadOrder = reader.ReadUInt32();
            reader.BaseStream.Position += 4; // Padding

            var pos = reader.BaseStream.Position;
            var packageStoreArrayNum = reader.ReadInt32();
            var packageStoreOffsetToData = reader.ReadUInt32();
            reader.BaseStream.Position = pos + packageStoreOffsetToData;
            ImportedPackages = new FPackageId[packageStoreArrayNum];

            for (var i = 0; i < packageStoreArrayNum; i++)
            {
                ImportedPackages[i] = new FPackageId(reader);
            }

            reader.BaseStream.Position = pos + 8;
        }
    }
}