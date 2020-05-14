using System.IO;

namespace PakReader.Parsers.Objects
{
    public readonly struct FVector2D : IUStruct
    {
        public readonly float X;
        public readonly float Y;

        internal FVector2D(BinaryReader reader)
        {
            X = reader.ReadSingle();
            Y = reader.ReadSingle();
        }
    }
}
