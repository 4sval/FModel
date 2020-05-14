namespace PakReader.Parsers.Objects
{
    public readonly struct FPerPlatformFloat : IUStruct
    {
        public readonly float Default;

        internal FPerPlatformFloat(PackageReader reader)
        {
            _ = reader.ReadByte() != 0; //bCooked
            Default = reader.ReadFloat();
        }
    }
}
