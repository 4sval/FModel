namespace PakReader.Parsers.Objects
{
    public readonly struct FEntry : IUStruct
    {
        /** The index into Items of the first item */
        public readonly int StartIndex;
        /** The number of currently valid items */
        public readonly int Size;
        /** The total capacity of allowed items before reallocating */
        public readonly int Capacity;

        internal FEntry(PackageReader reader)
        {
            StartIndex = reader.ReadInt32();
            Size = reader.ReadInt32();
            Capacity = reader.ReadInt32();
        }
    }
}
