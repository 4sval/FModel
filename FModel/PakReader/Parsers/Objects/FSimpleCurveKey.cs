namespace PakReader.Parsers.Objects
{
    public readonly struct FSimpleCurveKey : IUStruct
    {
        /** Time at this key */
        public readonly float KeyTime;
        /** Value at this key */
        public readonly float KeyValue;

        internal FSimpleCurveKey(PackageReader reader)
        {
            KeyTime = reader.ReadFloat();
            KeyValue = reader.ReadFloat();
        }
    }
}
