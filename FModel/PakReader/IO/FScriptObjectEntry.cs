using System.IO;

namespace FModel.PakReader.IO
{
    public readonly struct FScriptObjectEntry
    {
        public readonly FMinimalName ObjectName;
        public readonly FPackageObjectIndex GlobalIndex;
        public readonly FPackageObjectIndex OuterIndex;
        public readonly FPackageObjectIndex CDOClassIndex;

        public FScriptObjectEntry(BinaryReader reader)
        {
            ObjectName = new FMinimalName(reader);
            GlobalIndex = new FPackageObjectIndex(reader);
            OuterIndex = new FPackageObjectIndex(reader);
            CDOClassIndex = new FPackageObjectIndex(reader);
        }
    }
}
