using FModel.Utils;
using System.Collections.Generic;

namespace PakReader.Parsers.Objects
{
    public readonly struct FStringTable : IUStruct
    {
        /** The namespace to use for all the strings in this table */
        public readonly string TableNamespace;

        public readonly Dictionary<string, Dictionary<string, string>> KeysToMetadata;

        internal FStringTable(PackageReader reader)
        {
            TableNamespace = reader.ReadFString();
            KeysToMetadata = new Dictionary<string, Dictionary<string, string>>
            {
                { TableNamespace, new Dictionary<string, string>() }
            };

            int NumEntries = reader.ReadInt32();
            for (int i = 0; i < NumEntries; i++)
            {
                string key = reader.ReadFString();
                KeysToMetadata[TableNamespace].Add(key, Localizations.GetLocalization(TableNamespace, key, reader.ReadFString()));
            }
        }
    }
}
