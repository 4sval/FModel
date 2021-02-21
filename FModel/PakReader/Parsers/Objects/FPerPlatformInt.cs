namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct FPerPlatformInt : IUStruct
    {
        public readonly bool bCooked;
        public readonly int Default;

        internal FPerPlatformInt(PackageReader reader)
        {
            bCooked = reader.ReadInt32() != 0;
            Default = reader.ReadInt32();
        }
    }
}
