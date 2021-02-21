namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct FPerPlatformFloat : IUStruct
    {
        public readonly bool bCooked;
        public readonly float Default;

        internal FPerPlatformFloat(PackageReader reader)
        {
            bCooked = reader.ReadInt32() != 0;
            Default = reader.ReadFloat();
        }
    }
}
