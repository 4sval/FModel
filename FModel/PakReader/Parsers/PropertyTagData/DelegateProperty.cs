using PakReader.Parsers.Objects;
using System.Collections.Generic;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class DelegateProperty : BaseProperty
    {
        public int Object;
        public FName Name;

        internal DelegateProperty(PackageReader reader)
        {
            Object = reader.ReadInt32();
            Name = reader.ReadFName();
        }

        public Dictionary<string, object> GetValue() => new Dictionary<string, object> { ["Object"] = Object, ["Name"] = Name.String };
    }
}
