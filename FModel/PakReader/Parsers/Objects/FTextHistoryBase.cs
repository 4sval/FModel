using FModel.Utils;

namespace PakReader.Parsers.Objects
{
    public partial class FTextHistory
    {
        public sealed class Base : FTextHistory
        {
            // This is the base class for text histories
            public readonly string Namespace;
            public readonly string Key;
            public readonly string SourceString;

            internal Base(PackageReader reader)
            {
                Namespace = reader.ReadFString() ?? string.Empty; // namespaces are sometimes null
                Key = reader.ReadFString() ?? string.Empty;
                SourceString = Localizations.GetLocalization(Namespace, Key, reader.ReadFString());
            }
        }
    }
}