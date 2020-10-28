namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct FMovieSceneEvaluationKey : IUStruct
    {
        public readonly uint SequenceId;
        public readonly int TrackId;
        public readonly uint SectionIndex;

        internal FMovieSceneEvaluationKey(PackageReader reader)
        {
            SequenceId = reader.ReadUInt32();
            TrackId = reader.ReadInt32();
            SectionIndex = reader.ReadUInt32();
        }
    }
}
