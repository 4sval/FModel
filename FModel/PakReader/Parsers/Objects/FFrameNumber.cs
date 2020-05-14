namespace PakReader.Parsers.Objects
{
    public readonly struct FFrameNumber : IUStruct
    {
        public readonly float Value;

        internal FFrameNumber(PackageReader reader)
        {
            Value = reader.ReadFloat();
        }
    }
}
