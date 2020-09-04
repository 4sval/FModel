
namespace PakReader.Parsers.Objects
{
    public enum ETransformType : byte
    {
        ToLower = 0,
        ToUpper,

        // Add new enum types at the end only! They are serialized by index.
    }
}
