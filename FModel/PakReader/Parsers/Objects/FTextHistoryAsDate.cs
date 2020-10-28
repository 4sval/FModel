using System.Collections.Generic;

namespace FModel.PakReader.Parsers.Objects
{
    public partial class FTextHistory
    {
        public sealed class AsDate : FTextHistory
        {
            public readonly FDateTime SourceDateTime;
            public readonly EDateTimeStyle DateStyle;
            public readonly EDateTimeStyle TimeStyle;
            public readonly string TimeZone;
            public readonly string CultureName;

            internal AsDate(PackageReader reader)
            {
                SourceDateTime = new FDateTime(reader);
                DateStyle = (EDateTimeStyle)reader.ReadByte();
                TimeStyle = (EDateTimeStyle)reader.ReadByte();
                TimeZone = reader.ReadFString();
                CultureName = reader.ReadFString();
            }

            public Dictionary<string, object> GetValue()
            {
                return new Dictionary<string, object>
                {
                    ["SourceDateTime"] = SourceDateTime.Ticks,
                    ["DateStyle"] = DateStyle,
                    ["TimeStyle"] = TimeStyle,
                    ["TimeZone"] = TimeZone,
                    ["CultureName"] = CultureName,
                };
            }
        }
    }
}
