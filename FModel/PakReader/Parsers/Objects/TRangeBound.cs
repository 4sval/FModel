namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct TRangeBound<T> : IUStruct
    {
        public readonly ERangeBoundType BoundType;
        public readonly T Value;

        internal TRangeBound(PackageReader reader)
        {
            BoundType = (ERangeBoundType)reader.ReadByte();
            Value = default;
        }
    }
}
