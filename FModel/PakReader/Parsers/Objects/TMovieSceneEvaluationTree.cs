namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct TMovieSceneEvaluationTree<T>
    {
        public readonly FMovieSceneEvaluationTree BaseTree;
        public readonly TEvaluationTreeEntryContainer<T> Data;

        internal TMovieSceneEvaluationTree(PackageReader reader)
        {
            BaseTree = new FMovieSceneEvaluationTree(reader);
            Data = new TEvaluationTreeEntryContainer<T>(reader);
        }
    }
}
