namespace PakReader.Parsers.Objects
{
    public readonly struct FMovieSceneEvaluationTree : IUStruct
    {
        public readonly FMovieSceneEvaluationTreeNode RootNode;
        public readonly TEvaluationTreeEntryContainer<FMovieSceneEvaluationTreeNode> ChildNodes;

        internal FMovieSceneEvaluationTree(PackageReader reader)
        {
            RootNode = new FMovieSceneEvaluationTreeNode(reader);
            ChildNodes = new TEvaluationTreeEntryContainer<FMovieSceneEvaluationTreeNode>(reader);
        }
    }
}
