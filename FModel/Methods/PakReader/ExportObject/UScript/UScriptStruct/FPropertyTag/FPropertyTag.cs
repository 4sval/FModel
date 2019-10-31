using Newtonsoft.Json;

namespace PakReader
{
    public struct FPropertyTag
    {
        public string name;
        public long position;
        [JsonIgnore]
        public string property_type;
        public object tag_data;
        [JsonIgnore]
        public int size;
        [JsonIgnore]
        public int array_index;
        [JsonIgnore]
        public FGuid property_guid;
        [JsonIgnore]
        public FPropertyTagType tag;

        public bool Equals(FPropertyTag b)
        {
            return name == b.name &&
                position == b.position &&
                property_type == b.property_type &&
                size == b.size &&
                array_index == b.array_index &&
                tag == b.tag &&
                tag_data == b.tag_data;
        }
    }
}
