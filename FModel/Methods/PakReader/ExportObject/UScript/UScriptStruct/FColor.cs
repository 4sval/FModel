using Newtonsoft.Json;
using System.IO;
using static PakReader.AssetReader;

namespace PakReader
{
    public struct FColor
    {
        public byte r;
        public byte g;
        public byte b;
        public byte a;

        [JsonProperty]
        public string Hex => a == 0 || a == 255 ?
            ToHex(r, g, b) :
            ToHex(a, r, g, b);

        internal FColor(BinaryReader reader)
        {
            r = reader.ReadByte();
            g = reader.ReadByte();
            b = reader.ReadByte();
            a = reader.ReadByte();
        }
    }
}
