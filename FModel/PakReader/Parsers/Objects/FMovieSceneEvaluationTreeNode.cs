namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct FMovieSceneEvaluationTreeNode : IUStruct
    {
        public readonly TRange<int> Range;
        public readonly FMovieSceneEvaluationTreeNodeHandle Parent;
        public readonly FEvaluationTreeEntryHandle ChildrenId;
        public readonly FEvaluationTreeEntryHandle DataId;

        internal FMovieSceneEvaluationTreeNode(PackageReader reader)
        {
            Range = new TRange<int>(reader);
            Parent = new FMovieSceneEvaluationTreeNodeHandle(reader);
            ChildrenId = new FEvaluationTreeEntryHandle(reader);
            DataId = new FEvaluationTreeEntryHandle(reader);
        }
    }
}
