using System.Collections.Generic;
using System.IO;

namespace FModel.PakReader.IO
{
    public class FContainerHeader
    {
        public readonly FIoContainerId ContainerId;
        public readonly uint PackageCount;
        public readonly byte[] Names;
        public readonly byte[] NameHashes;
        public readonly FPackageId[] PackageIds;
        public readonly byte[] StoreEntries;
        public readonly Dictionary<string, (FPackageId source, FPackageId localized)[]> CulturePackageMap;
        public readonly (FPackageId source, FPackageId target)[] PackageRedirects;

        public FContainerHeader(BinaryReader reader)
        {
            ContainerId = new FIoContainerId(reader);
            PackageCount = reader.ReadUInt32();
            Names = reader.ReadBytes(reader.ReadInt32());
            NameHashes = reader.ReadBytes(reader.ReadInt32());
            PackageIds = reader.ReadTArray(() => new FPackageId(reader));
            StoreEntries = reader.ReadBytes(reader.ReadInt32());
            var culturePackageMapCount = reader.ReadInt32();
            CulturePackageMap = new Dictionary<string, (FPackageId source, FPackageId localized)[]>(culturePackageMapCount);
            for (int i = 0; i < culturePackageMapCount; i++)
            {
                CulturePackageMap.Add(
                    reader.ReadFString(), 
                    reader.ReadTArray(() => (new FPackageId(reader), new FPackageId(reader))));
            }

            PackageRedirects = reader.ReadTArray(() => (new FPackageId(reader), new FPackageId(reader)));
        }
    }
}