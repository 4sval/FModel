using System.Collections.Generic;

namespace PakReader.Parsers.Objects
{
    public readonly struct FAssetPackageData : IUStruct
    {
        public readonly FName PackageName;
        /** Total size of this asset on disk */
        public readonly long DiskSize;
        /** Guid of the source package, uniquely identifies an asset package */
        public readonly FGuid PackageGuid;
        /** MD5 of the cooked package on disk, for tracking nondeterministic changes */
        public readonly FMD5Hash CookedHash;

        internal FAssetPackageData(FNameTableArchiveReader reader, bool bSerializeHash)
        {
            PackageName = reader.ReadFName();
            DiskSize = reader.Loader.ReadInt64();
            PackageGuid = new FGuid(reader.Loader);
            if (bSerializeHash)
                CookedHash = new FMD5Hash(reader.Loader);
            else
                CookedHash = default;
        }

        public Dictionary<string, object> GetValue()
        {
            return new Dictionary<string, object>
            {
                ["PackageName"] = PackageName.String,
                ["DiskSize"] = DiskSize,
                ["PackageGuid"] = PackageGuid.Hex,
                ["CookedHash"] = CookedHash
            };
        }
    }
}
