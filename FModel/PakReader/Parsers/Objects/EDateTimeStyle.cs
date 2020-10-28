namespace FModel.PakReader.Parsers.Objects
{
    public enum EDateTimeStyle : byte
    {
        Default,
        Short,
        Medium,
        Long,
        Full
        // Add new enum types at the end only! They are serialized by index.
    }
}
