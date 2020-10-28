using System.Collections.Generic;

namespace FModel.PakReader.Parsers.Objects
{
    public partial class FTextHistory
    {
        public sealed class StringTableEntry : FTextHistory
        {
            /** The string table ID being referenced */
            public readonly FName TableId;
            /** The key within the string table being referenced */
            public readonly string Key;

            internal StringTableEntry(PackageReader reader)
            {
                TableId = reader.ReadFName();
                Key = reader.ReadFString();
            }

            public Dictionary<string, string> GetValue()
            {
                return new Dictionary<string, string>
                {
                    ["TableId"] = TableId.String,
                    ["Key"] = Key
                };
            }
        }
    }
}
