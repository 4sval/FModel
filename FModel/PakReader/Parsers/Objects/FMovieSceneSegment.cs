namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct FMovieSceneSegment : IUStruct
    {
        /** The segment's range */
        public readonly TRange<FFrameNumber> Range;
        public readonly int ID;
        /** Whether this segment has been generated yet or not */
        public readonly bool bAllowEmpty;
        /** Array of implementations that reside at the segment's range */
        public readonly UScriptStruct[] Impls;

        internal FMovieSceneSegment(PackageReader reader)
        {
            Range = new TRange<FFrameNumber>(reader);
            ID = reader.ReadInt32();
            bAllowEmpty = reader.ReadUInt32() != 0;
            Impls = new UScriptStruct[reader.ReadUInt32()];
            for (int i = 0; i < Impls.Length; i++)
            {
                Impls[i] = new UScriptStruct(reader, "SectionEvaluationData");
            }
        }
    }
}
