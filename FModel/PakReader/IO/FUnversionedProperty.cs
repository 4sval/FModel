using System.Collections.Generic;
using UsmapNET.Classes;

namespace FModel.PakReader.IO
{
    public class FUnversionedType
    {
        public string Name { get; }
        public Dictionary<int, FUnversionedProperty> Properties { get; set; }

        public FUnversionedType(string name)
        {
            Name = name;
            Properties = new Dictionary<int, FUnversionedProperty>();
        }
    }

    public class FUnversionedProperty
    {
        public string Name { get; set; }
        public UsmapPropertyData Data { get; set; }

        public FUnversionedProperty(UsmapProperty prop)
        {
            Name = prop.Name;
            Data = prop.Data;
        }
    }
}
