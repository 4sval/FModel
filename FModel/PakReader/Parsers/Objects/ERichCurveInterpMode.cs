namespace PakReader.Parsers.Objects
{
    /** Method of interpolation between this key and the next. */
    public enum ERichCurveInterpMode : byte
    {
        /** Use linear interpolation between values. */
        Linear,
        /** Use a constant value. Represents stepped values. */
        Constant,
        /** Cubic interpolation. See TangentMode for different cubic interpolation options. */
        Cubic,
        /** No interpolation. */
        None
    }
}
