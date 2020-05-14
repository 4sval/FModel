namespace PakReader.Parsers.Objects
{
    public readonly struct FNavAgentSelectorCustomization : IUStruct
    {
        public readonly FText SupportedDesc;

        internal FNavAgentSelectorCustomization(PackageReader reader)
        {
            SupportedDesc = new FText(reader);
        }
    }
}
