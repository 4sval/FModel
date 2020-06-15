namespace PakReader.Parsers.Objects
{
    public readonly struct FBox : IUStruct
    {
        /** Holds the box's minimum point. */
        public readonly FVector Min;
        /** Holds the box's maximum point. */
        public readonly FVector Max;
        /** Holds a flag indicating whether this box is valid. */
        public readonly bool bIsValid;

        internal FBox(PackageReader reader)
        {
            Min = new FVector(reader);
            Max = new FVector(reader);
            bIsValid = reader.ReadByte() != 0;
        }
    }
}
