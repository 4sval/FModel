using Newtonsoft.Json;

namespace FModel.PakReader.IO
{
    public class PropertyInfo
    {
        public string Name;
        public string Type;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string StructType;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? Bool;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EnumName;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EnumType;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string InnerType;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ValueType;

        public PropertyInfo(string name, string type, string structType = null, bool? b = null, string enumName = null, string enumType = null, string innerType = null, string valueType = null)
        {
            Name = name;
            Type = type;
            StructType = structType;
            Bool = b;
            EnumName = enumName;
            EnumType = enumType;
            InnerType = innerType;
            ValueType = valueType;
        }
    }
}