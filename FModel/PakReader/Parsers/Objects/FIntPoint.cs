using System.IO;

namespace PakReader.Parsers.Objects
{
    public readonly struct FIntPoint : IUStruct
    {
        public readonly int X;
        public readonly int Y;

        internal FIntPoint(BinaryReader reader)
        {
            X = reader.ReadInt32();
            Y = reader.ReadInt32();
        }
    }
}
