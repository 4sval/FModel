namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct FMovieSceneEvaluationTreeNodeHandle : IUStruct
    {
        public readonly FEvaluationTreeEntryHandle ChildrenHandle;
        public readonly int Index;

        internal FMovieSceneEvaluationTreeNodeHandle(PackageReader reader)
        {
            ChildrenHandle = new FEvaluationTreeEntryHandle(reader);
            Index = reader.ReadInt32();
        }
    }
}
