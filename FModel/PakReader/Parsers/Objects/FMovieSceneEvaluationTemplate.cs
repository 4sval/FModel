namespace PakReader.Parsers.Objects
{
    public readonly struct FMovieSceneEvaluationTemplate : IUStruct
    {
        /** The internal value of the serial number */
        public readonly uint Value;

        internal FMovieSceneEvaluationTemplate(PackageReader reader)
        {
            Value = reader.ReadUInt32();
        }
    }
}
