using System.Collections.Generic;

namespace PakReader.Parsers.Objects
{
    public partial class FTextHistory
    {
        public sealed class DateTime : FTextHistory
        {
            public readonly FDateTime SourceDateTime;
            public readonly EDateTimeStyle DateStyle;
            public readonly EDateTimeStyle TimeStyle;
            public readonly string TimeZone;
            // UE4 converts the string into an FCulturePtr
            // https://github.com/EpicGames/UnrealEngine/blob/bf95c2cbc703123e08ab54e3ceccdd47e48d224a/Engine/Source/Runtime/Core/Private/Internationalization/TextHistory.cpp#L2188
            public readonly string TargetCulture;

            internal DateTime(PackageReader reader)
            {
                SourceDateTime = new FDateTime(reader);
                DateStyle = (EDateTimeStyle)reader.ReadByte();
                TimeStyle = (EDateTimeStyle)reader.ReadByte();
                TimeZone = reader.ReadFString();
                TargetCulture = reader.ReadFString();
            }

            public Dictionary<string, object> GetValue()
            {
                return new Dictionary<string, object>
                {
                    ["SourceDateTime"] = SourceDateTime.Ticks,
                    ["DateStyle"] = DateStyle,
                    ["TimeStyle"] = TimeStyle,
                    ["TimeZone"] = TimeZone,
                    ["TargetCulture"] = TargetCulture,
                };
            }
        }
    }
}
