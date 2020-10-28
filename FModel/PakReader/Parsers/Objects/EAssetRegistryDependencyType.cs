namespace FModel.PakReader.Parsers.Objects
{
    public enum EAssetRegistryDependencyType
    {
		// Dependencies which don't need to be loaded for the object to be used (i.e. soft object paths)
		Soft = 0x01,

		// Dependencies which are required for correct usage of the source asset, and must be loaded at the same time
		Hard = 0x02,

		// References to specific SearchableNames inside a package
		SearchableName = 0x04,

		// Indirect management references, these are set through recursion for Primary Assets that manage packages or other primary assets
		SoftManage = 0x08,

		// Reference that says one object directly manages another object, set when Primary Assets manage things explicitly
		HardManage = 0x10
	}
}
