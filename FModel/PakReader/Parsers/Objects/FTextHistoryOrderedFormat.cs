namespace PakReader.Parsers.Objects
{
    public partial class FTextHistory
    {
        public sealed class OrderedFormat : FTextHistory
        {
            /** The pattern used to format the text */
            public readonly FText SourceFmt;
            /** Arguments to replace in the pattern string */
            public readonly FFormatArgumentValue[] Arguments;
            
            internal OrderedFormat(PackageReader reader)
            {
                SourceFmt = new FText(reader);
                Arguments = new FFormatArgumentValue[reader.ReadInt32()];
                for (int i = 0; i < Arguments.Length; i++)
                {
                    Arguments[i] = new FFormatArgumentValue(reader);
                }
            }
        }
    }
}
