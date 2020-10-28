using System.Collections.Generic;

namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct FAssetData : IUStruct
    {
		/** The object path for the asset in the form PackageName.AssetName. Only top level objects in a package can have AssetData */
		public readonly FName ObjectPath;
		/** The name of the package in which the asset is found, this is the full long package name such as /Game/Path/Package */
		public readonly FName PackageName;
		/** The path to the package in which the asset is found, this is /Game/Path with the Package stripped off */
		public readonly FName PackagePath;
		/** The name of the asset without the package */
		public readonly FName AssetName;
		/** The name of the asset's class */
		public readonly FName AssetClass;
		/** The map of values for properties that were marked AssetRegistrySearchable or added by GetAssetRegistryTags */
		public readonly FAssetDataTagMapSharedView TagsAndValues;
		/** The IDs of the chunks this asset is located in for streaming install.  Empty if not assigned to a chunk */
		public readonly int[] ChunkIDs;
		/** Asset package flags */
		public readonly int PackageFlags;

		internal FAssetData(FNameTableArchiveReader reader)
		{
			ObjectPath = reader.ReadFName();
			PackagePath = reader.ReadFName();
			AssetClass = reader.ReadFName();

			PackageName = reader.ReadFName();
			AssetName = reader.ReadFName();

			TagsAndValues = new FAssetDataTagMapSharedView(reader);
			ChunkIDs = reader.Loader.ReadTArray(() => reader.Loader.ReadInt32());
			PackageFlags = reader.Loader.ReadInt32();
		}

		public Dictionary<string, object> GetValue()
		{
			return new Dictionary<string, object>
			{
				["ObjectPath"] = ObjectPath.String,
				["PackageName"] = PackageName.String,
				["PackagePath"] = PackagePath.String,
				["AssetName"] = AssetName.String,
				["AssetClass"] = AssetClass.String,
				["TagsAndValues"] = TagsAndValues.Map,
				["ChunkIDs"] = ChunkIDs,
				["PackageFlags"] = PackageFlags
			};
		}
	}
}
