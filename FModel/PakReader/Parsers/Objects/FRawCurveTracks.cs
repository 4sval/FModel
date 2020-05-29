namespace PakReader.Parsers.Objects
{
    public readonly struct FRawCurveTracks : IUStruct
    {
        public readonly FFloatCurve[] FloatCurves;
        public readonly FVectorCurve[] VectorCurves;
        public readonly FTransformCurve[] TransformCurves;

        internal FRawCurveTracks(PackageReader reader)
        {
            FloatCurves = reader.ReadTArray(() => new FFloatCurve(reader));
            VectorCurves = reader.ReadTArray(() => new FVectorCurve(reader));
            TransformCurves = reader.ReadTArray(() => new FTransformCurve(reader));
        }
    }
}
