using System.IO;

namespace PakReader.Parsers.Objects
{
    public readonly struct FVector : IUStruct
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;

        internal FVector(BinaryReader reader)
        {
            X = reader.ReadSingle();
            Y = reader.ReadSingle();
            Z = reader.ReadSingle();
        }
    }
}
