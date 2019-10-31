using Newtonsoft.Json;
using System;
using System.IO;
using static PakReader.AssetReader;

namespace PakReader
{
    public struct FText
    {
        [JsonIgnore]
        public uint flags;
        [JsonIgnore]
        public byte history_type;
        public string @namespace;
        public string key;
        public string source_string;

        internal FText(BinaryReader reader)
        {
            flags = reader.ReadUInt32();
            history_type = reader.ReadByte();

            if (history_type == 255)
            {
                @namespace = "";
                key = "";
                source_string = "";
            }
            else if (history_type == 0)
            {
                @namespace = read_string(reader);
                key = read_string(reader);
                source_string = read_string(reader);
            }
            else
            {
                throw new NotImplementedException($"Could not read history type: {history_type}");
            }
        }
    }
}
