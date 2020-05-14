namespace PakReader.Parsers.Objects
{
    /** Enumerates tangent weight modes. */
    public enum ERichCurveTangentWeightMode
    {
        /** Don't take tangent weights into account. */
        None,
        /** Only take the arrival tangent weight into account for evaluation. */
        Arrive,
        /** Only take the leaving tangent weight into account for evaluation. */
        Leave,
        /** Take both the arrival and leaving tangent weights into account for evaluation. */
        Both
    }
}
