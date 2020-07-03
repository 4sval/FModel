using System.Collections.Generic;

namespace PakReader.Parsers.Objects
{
    public partial class FTextHistory
    {
        public sealed class FormatNumber : FTextHistory
        {
            /** The source value to format from */
            public readonly FFormatArgumentValue SourceValue;
            /** The culture to format using */
            public readonly string TimeZone;
            public readonly string TargetCulture;

            internal FormatNumber(PackageReader reader)
            {
                SourceValue = new FFormatArgumentValue(reader);
                TimeZone = reader.ReadFString();
                TargetCulture = reader.ReadFString();
            }

            public Dictionary<string, object> GetValue()
            {
                return new Dictionary<string, object>
                {
                    ["SourceValue"] = SourceValue.GetValue(),
                    ["TimeZone"] = TimeZone,
                    ["TargetCulture"] = TargetCulture
                };
            }
        }
    }
}
