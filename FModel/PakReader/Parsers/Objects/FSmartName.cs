namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct FSmartName : IUStruct
    {
        // name 
        public readonly FName DisplayName;

        internal FSmartName(PackageReader reader)
        {
            DisplayName = reader.ReadFName();
        }
    }
}
