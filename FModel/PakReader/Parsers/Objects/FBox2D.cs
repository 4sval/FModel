namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct FBox2D : IUStruct
    {
        /** Holds the box's minimum point. */
        public readonly FVector2D Min;
        /** Holds the box's maximum point. */
        public readonly FVector2D Max;
        /** Holds a flag indicating whether this box is valid. */
        public readonly bool bIsValid;

        internal FBox2D(PackageReader reader)
        {
            Min = new FVector2D(reader);
            Max = new FVector2D(reader);
            bIsValid = reader.ReadByte() != 0;
        }
    }
}
