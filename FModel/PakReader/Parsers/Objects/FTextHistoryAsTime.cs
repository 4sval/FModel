using System.Collections.Generic;

namespace PakReader.Parsers.Objects
{
    public partial class FTextHistory
    {
        public sealed class AsTime : FTextHistory
        {
            public readonly FDateTime SourceDateTime;
            public readonly EDateTimeStyle TimeStyle;
            public readonly string TimeZone;
            public readonly string CultureName;

            internal AsTime(PackageReader reader)
            {
                SourceDateTime = new FDateTime(reader);
                TimeStyle = (EDateTimeStyle)reader.ReadByte();
                TimeZone = reader.ReadFString();
                CultureName = reader.ReadFString();
            }

            public Dictionary<string, object> GetValue()
            {
                return new Dictionary<string, object>
                {
                    ["SourceDateTime"] = SourceDateTime.Ticks,
                    ["TimeStyle"] = TimeStyle,
                    ["TimeZone"] = TimeZone,
                    ["CultureName"] = CultureName,
                };
            }
        }
    }
}
