namespace PakReader.Parsers.Objects
{
    public readonly struct FVectorCurve : IUStruct
    {
        /** Curve data for float. */
        public readonly FRichCurveKey[] FloatCurves; // actually FRichCurve

        internal FVectorCurve(PackageReader reader)
        {
            FloatCurves = new FRichCurveKey[3];
            for (int i = 0; i < FloatCurves.Length; i++)
            {
                FloatCurves[i] = new FRichCurveKey(reader);
            }
        }
    }
}
