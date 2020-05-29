namespace PakReader.Parsers.Objects
{
    public readonly struct FFloatCurve : IUStruct
    {
        /** Curve data for float. */
        public readonly FRichCurveKey[] FloatCurve; // actually FRichCurve

        internal FFloatCurve(PackageReader reader)
        {
            FloatCurve = reader.ReadTArray(() => new FRichCurveKey(reader));
        }
    }
}
