namespace PakReader.Parsers.Objects
{
    /** Enumerates curve compression options. */
    public enum ERichCurveCompressionFormat
    {
        /** No keys are present */
        Empty,
        /** All keys use constant interpolation */
        Constant,
        /** All keys use linear interpolation */
        Linear,
        /** All keys use cubic interpolation */
        Cubic,
        /** Keys use mixed interpolation modes */
        Mixed
    }
}
