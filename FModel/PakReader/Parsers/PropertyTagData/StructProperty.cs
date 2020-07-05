using PakReader.Parsers.Class;
using PakReader.Parsers.Objects;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class StructProperty : BaseProperty<IUStruct>
    {
        internal StructProperty(PackageReader reader, FPropertyTag tag)
        {
            Position = reader.Position;
            Value = new UScriptStruct(reader, tag.StructName).Struct;
        }

        public object GetValue()
        {
            return Value switch
            {
                UObject uObject => uObject.GetJsonDict(),
                FAssetData fAssetData => fAssetData.GetValue(),
                FAssetDataTagMapSharedView fAssetDataTagMapSharedView => fAssetDataTagMapSharedView.Map,
                FAssetIdentifier fAssetIdentifier => fAssetIdentifier.GetValue(),
                FAssetPackageData fAssetPackageData => fAssetPackageData.GetValue(),
                FGameplayTagContainer fGameplayTagContainer => fGameplayTagContainer.GetValue(),
                FSoftObjectPath fSoftObjectPath => fSoftObjectPath.GetValue(),
                FGuid fGuid => fGuid.Hex,
                _ => Value
            };
        }
    }
}
