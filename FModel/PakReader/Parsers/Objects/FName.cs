using Newtonsoft.Json;

namespace PakReader.Parsers.Objects
{
    public readonly struct FName
    {
        readonly FNameEntrySerialized Name;
        [JsonIgnore]
        public readonly int Index;
        [JsonIgnore]
        public readonly int Number;

        public string String => Name.Name;

        [JsonIgnore]
        public bool IsNone => String == "None";

        internal FName(FNameEntrySerialized name, int index, int number)
        {
            Name = name;
            Index = index;
            Number = number;
        }

        public override string ToString() => Name.Name;
    }
}
