using System.IO;

namespace PakReader.Parsers.Objects
{
    public readonly struct FAssetRegistryVersion : IUStruct
    {
        public enum Type
        {
            PreVersioning = 0,      // From before file versioning was implemented
            HardSoftDependencies,   // The first version of the runtime asset registry to include file versioning.
            AddAssetRegistryState,  // Added FAssetRegistryState and support for piecemeal serialization
            ChangedAssetData,       // AssetData serialization format changed, versions before this are not readable
            RemovedMD5Hash,         // Removed MD5 hash from package data
            AddedHardManage,        // Added hard/soft manage references
            AddedCookedMD5Hash,     // Added MD5 hash of cooked package to package data

            // -----<new versions can be added above this line>-------------------------------------------------
            VersionPlusOne,
            LatestVersion = VersionPlusOne - 1
        }

        static readonly FGuid GUID = new FGuid(0x717F9EE7, 0xE9B0493A, 0x88B39132, 0x1B388107);

        public static Type DeserializeVersion(BinaryReader reader)
        {
            if (GUID == new FGuid(reader))
                return (Type)reader.ReadInt32();

            return default;
        }
    }
}
