namespace PakReader.Parsers.Objects
{
    public readonly struct FPerPlatformInt : IUStruct
    {
        public readonly int Default;

        internal FPerPlatformInt(PackageReader reader)
        {
            _ = reader.ReadByte() != 0; //bCooked
            Default = reader.ReadInt32();
        }
    }
}
