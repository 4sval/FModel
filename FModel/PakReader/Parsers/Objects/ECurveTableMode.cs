namespace PakReader.Parsers.Objects
{
    /**
     * Whether the curve table contains simple, rich, or no curves
     */
    public enum ECurveTableMode : byte
    {
        Empty,
        SimpleCurves,
        RichCurves,
    }
}
