using FModel.PakReader.Parsers.Class;
using FModel.PakReader.Parsers.Objects;

namespace FModel.PakReader.Parsers.PropertyTagData
{
    public sealed class StructProperty : BaseProperty<IUStruct>
    {
        internal StructProperty(FPropertyTag tag)
        {
            Value = null;
            Value = new UScriptStruct(tag.StructName.String).Struct;
        }
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
