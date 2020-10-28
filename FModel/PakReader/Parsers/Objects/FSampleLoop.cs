using System.IO;

namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct FSampleLoop : IUStruct
    {
        public readonly uint Identifier;
        public readonly uint Type;
        public readonly uint Start;
        public readonly uint End;
        public readonly uint Fraction;
        public readonly uint PlayCount;

        internal FSampleLoop(BinaryReader reader)
        {
            Identifier = reader.ReadUInt32();
            Type = reader.ReadUInt32();
            Start = reader.ReadUInt32();
            End = reader.ReadUInt32();
            Fraction = reader.ReadUInt32();
            PlayCount = reader.ReadUInt32();
        }
    }
}
