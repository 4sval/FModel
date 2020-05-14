using System;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;

namespace PakReader.Parsers.Objects
{
    public readonly struct FGuid : IUStruct, IEquatable<FGuid>
    {
        [JsonIgnore]
        public readonly uint A;
        [JsonIgnore]
        public readonly uint B;
        [JsonIgnore]
        public readonly uint C;
        [JsonIgnore]
        public readonly uint D;

        public string Hex => ToString();

        private static readonly FGuid zero = new FGuid(0, 0, 0, 0);
        public static ref readonly FGuid Zero => ref zero;

        internal FGuid(uint a, uint b, uint c, uint d)
        {
            A = a;
            B = b;
            C = c;
            D = d;
        }

        internal FGuid(string guid)
        {
            A = uint.Parse(guid[0 .. 8], NumberStyles.HexNumber);
            B = uint.Parse(guid[8 ..16], NumberStyles.HexNumber);
            C = uint.Parse(guid[16..24], NumberStyles.HexNumber);
            D = uint.Parse(guid[24..32], NumberStyles.HexNumber);
        }

        internal FGuid(BinaryReader reader)
        {
            A = reader.ReadUInt32();
            B = reader.ReadUInt32();
            C = reader.ReadUInt32();
            D = reader.ReadUInt32();
        }

        public bool IsValid() => (A | B | C | D) != 0;

        public bool Equals(FGuid b) => A == b.A && B == b.B && C == b.C && D == b.D;

        public override bool Equals(object obj) => obj is FGuid ? Equals((FGuid)obj) : false;

        public override int GetHashCode() => (int)(A ^ B ^ C ^ D);

        public static bool operator ==(FGuid left, FGuid right) => left.Equals(right);

        public static bool operator !=(FGuid left, FGuid right) => !left.Equals(right);

        // TODO: maybe make this more performant?
        public override string ToString() => $"{A}-{B}-{C}-{D}";
    }
}
