namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct FRichCurveKey : IUStruct
    {
        /** Interpolation mode between this key and the next */
        public readonly ERichCurveInterpMode InterpMode;
        /** Mode for tangents at this key */
        public readonly ERichCurveTangentMode TangentMode;
        /** If either tangent at this key is 'weighted' */
        public readonly ERichCurveTangentWeightMode TangentWeightMode;
        /** Time at this key */
        public readonly float KeyTime;
        /** Value at this key */
        public readonly float KeyValue;
        /** If Cubic, the arriving tangent at this key */
        public readonly float ArriveTangent;
        /** If WeightedArrive or WeightedBoth, the weight of the left tangent */
        public readonly float ArriveTangentWeight;
        /** If Cubic, the leaving tangent at this key */
        public readonly float LeaveTangent;
        /** If WeightedLeave or WeightedBoth, the weight of the right tangent */
        public readonly float LeaveTangentWeight;

        internal FRichCurveKey(PackageReader reader)
        {
            InterpMode = (ERichCurveInterpMode)reader.ReadByte();
            TangentMode = (ERichCurveTangentMode)reader.ReadByte();
            TangentWeightMode = (ERichCurveTangentWeightMode)reader.ReadByte();
            KeyTime = reader.ReadFloat();
            KeyValue = reader.ReadFloat();
            ArriveTangent = reader.ReadFloat();
            ArriveTangentWeight = reader.ReadFloat();
            LeaveTangent = reader.ReadFloat();
            LeaveTangentWeight = reader.ReadFloat();
        }
    }
}
