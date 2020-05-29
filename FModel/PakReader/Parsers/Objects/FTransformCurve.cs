namespace PakReader.Parsers.Objects
{
    public readonly struct FTransformCurve : IUStruct
    {
        /** Curve data for each transform. */
        public readonly FVectorCurve TranslationCurve;

        internal FTransformCurve(PackageReader reader)
        {
            TranslationCurve = new FVectorCurve(reader);
        }
    }
}
