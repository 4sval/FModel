using System.Collections.Generic;

namespace PakReader.Parsers.Objects
{
    public readonly struct FAssetIdentifier : IUStruct
    {
		/** The name of the package that is depended on, this is always set unless PrimaryAssetType is */
		public readonly FName PackageName;
		/** The primary asset type, if valid the ObjectName is the PrimaryAssetName */
		public readonly FName PrimaryAssetType;
		/** Specific object within a package. If empty, assumed to be the default asset */
		public readonly FName ObjectName;
		/** Name of specific value being referenced, if ObjectName specifies a type such as a UStruct */
		public readonly FName ValueName;

		internal FAssetIdentifier(FNameTableArchiveReader reader)
		{
            PackageName = default;
            PrimaryAssetType = default;
            ObjectName = default;
            ValueName = default;

            byte FieldBits = reader.Loader.ReadByte();
            if ((FieldBits & (1 << 0)) != 0)
            {
                PackageName = reader.ReadFName();
            }
            if ((FieldBits & (1 << 1)) != 0)
            {
                PrimaryAssetType = reader.ReadFName();
            }
            if ((FieldBits & (1 << 2)) != 0)
            {
                ObjectName = reader.ReadFName();
            }
            if ((FieldBits & (1 << 3)) != 0)
            {
                ValueName = reader.ReadFName();
            }
        }

        public Dictionary<string, string> GetValue()
        {
            return new Dictionary<string, string>
            {
                ["PackageName"] = PackageName.String,
                ["PrimaryAssetType"] = PrimaryAssetType.String,
                ["ObjectName"] = ObjectName.String,
                ["ValueName"] = ValueName.String
            };
        }
    }
}
