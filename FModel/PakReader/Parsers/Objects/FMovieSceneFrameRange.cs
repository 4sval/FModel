namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct FMovieSceneFrameRange : IUStruct
    {
        public readonly TRange<int> Value;

        internal FMovieSceneFrameRange(PackageReader reader)
        {
            Value = new TRange<int>(reader);
        }
    }
}
