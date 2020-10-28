using System.Collections.Generic;
using FModel.PakReader.Parsers.Objects;

namespace FModel.PakReader.Parsers.PropertyTagData
{
    public sealed class DelegateProperty : BaseProperty
    {
        public int Object;
        public FName Name;

        internal DelegateProperty()
        {
            Object = 0;
            Name = new FName();
        }

        internal DelegateProperty(PackageReader reader)
        {
            Object = reader.ReadInt32();
            Name = reader.ReadFName();
        }

        public Dictionary<string, object> GetValue() => new Dictionary<string, object> { ["Object"] = Object, ["Name"] = Name.String };
    }
}
