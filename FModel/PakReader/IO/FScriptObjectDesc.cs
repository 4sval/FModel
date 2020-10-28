using FModel.PakReader.Parsers.Objects;

namespace FModel.PakReader.IO
{
    public class FScriptObjectDesc
    {
        public readonly FName Name;
        public FName FullName;
        public readonly FPackageObjectIndex GlobalImportIndex;
        public readonly FPackageObjectIndex OuterIndex;

        public FScriptObjectDesc(FNameEntrySerialized name, FMappedName fMappedName, FScriptObjectEntry fScriptObjectEntry)
        {
            Name = new FName(name.Name, (int)fMappedName.Index, (int)fMappedName.Number);
            FullName = default;
            GlobalImportIndex = fScriptObjectEntry.GlobalIndex;
            OuterIndex = fScriptObjectEntry.OuterIndex;
        }
    }
}
