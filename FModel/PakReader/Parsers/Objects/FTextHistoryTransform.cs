using System.Collections.Generic;

namespace PakReader.Parsers.Objects
{
    public partial class FTextHistory
    {
        public sealed class Transform : FTextHistory
        {
            /** The source text instance that was transformed */
            public readonly FText SourceText;
            /** How the source text was transformed */
            public readonly ETransformType TransformType;

            internal Transform(PackageReader reader)
            {
                SourceText = new FText(reader);
                TransformType = (ETransformType)reader.ReadByte();
            }

            public Dictionary<string, object> GetValue()
            {
                return new Dictionary<string, object>
                {
                    ["SourceText"] = SourceText.GetValue(),
                    ["TransformType"] = TransformType
                };
            }
        }
    }
}
