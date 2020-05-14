namespace PakReader.Parsers.Objects
{
    public readonly struct FSectionEvaluationDataTree : IUStruct
    {
        public readonly TMovieSceneEvaluationTree<UScriptStruct> Tree;

        internal FSectionEvaluationDataTree(PackageReader reader)
        {
            Tree = new TMovieSceneEvaluationTree<UScriptStruct>(reader);
        }
    }
}
