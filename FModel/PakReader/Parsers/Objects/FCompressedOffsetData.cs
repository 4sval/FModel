namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct FCompressedOffsetData : IUStruct
    {
        public readonly int[] OffsetData;
        public readonly int StripSize;

        internal FCompressedOffsetData(PackageReader reader)
        {
            OffsetData = reader.ReadTArray(() => reader.ReadInt32());
            StripSize = reader.ReadInt32();
        }
    }
}
