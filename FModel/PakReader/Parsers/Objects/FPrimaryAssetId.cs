namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct FPrimaryAssetId : IUStruct
    {
        public readonly FPrimaryAssetType PrimaryAssetType;
        public readonly FName PrimaryAssetName;

        public FPrimaryAssetId(PackageReader reader)
        {
            PrimaryAssetType = new FPrimaryAssetType(reader);
            PrimaryAssetName = reader.ReadFName();
        }
    }
}