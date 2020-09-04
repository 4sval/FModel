using System.Collections.Generic;

namespace PakReader.Parsers.Objects
{
    public partial class FTextHistory
    {
        public sealed class ArgumentDataFormat : FTextHistory
        {
            public readonly FText FormatText;
            public readonly FFormatArgumentData[] Arguments;

            internal ArgumentDataFormat(PackageReader reader)
            {
                FormatText = new FText(reader);
                Arguments = reader.ReadTArray(() => new FFormatArgumentData(reader));
            }

            public Dictionary<string, object> GetValue()
            {
                var ret = new object[Arguments.Length];
                for (int i = 0; i < ret.Length; i++)
                {
                    ret[i] = Arguments[i] switch
                    {
                        _ => Arguments[i].GetValue(),
                    };
                }
                return new Dictionary<string, object>
                {
                    ["FormatText"] = FormatText.GetValue(),
                    ["Arguments"] = ret
                };
            }
        }
    }
}
