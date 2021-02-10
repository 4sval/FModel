using System.Collections.Generic;

namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct FSoftObjectPath : IUStruct
    {
        /** Asset path, patch to a top level object in a package. This is /package/path.assetname */
        public readonly FName AssetPathName;
        /** Optional FString for subobject within an asset. This is the sub path after the : */
        public readonly string SubPathString;

        internal FSoftObjectPath(PackageReader reader)
        {
            AssetPathName = reader.ReadFName();
            SubPathString = reader.ReadFString();
        }

        public Dictionary<string, string> GetValue()
        {
            return new Dictionary<string, string>
            {
                ["AssetPathName"] = AssetPathName.String,
                ["SubPathString"] = SubPathString
            };
        }
    }
}
