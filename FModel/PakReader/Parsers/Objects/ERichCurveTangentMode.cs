namespace PakReader.Parsers.Objects
{
    /** If using Cubic, this enum describes how the tangents should be controlled in editor. */
    public enum ERichCurveTangentMode : byte
    {
        /** Automatically calculates tangents to create smooth curves between values. */
        Auto,
        /** User specifies the tangent as a unified tangent where the two tangents are locked to each other, presenting a consistent curve before and after. */
        User,
        /** User specifies the tangent as two separate broken tangents on each side of the key which can allow a sharp change in evaluation before or after. */
        Break,
        /** No tangents. */
        None
    }
}
