namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct TRange<T>
    {
        public readonly TRangeBound<T> LowerBound;
        public readonly TRangeBound<T> UpperBound;

        internal TRange(PackageReader reader)
        {
            LowerBound = new TRangeBound<T>(reader);
            UpperBound = new TRangeBound<T>(reader);
        }
    }
}
