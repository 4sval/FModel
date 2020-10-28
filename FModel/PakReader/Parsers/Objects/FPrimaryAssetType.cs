namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct FPrimaryAssetType : IUStruct
    {
        public readonly FName Name;

        public FPrimaryAssetType(PackageReader reader)
        {
            Name = reader.ReadFName();
        }
    }
}