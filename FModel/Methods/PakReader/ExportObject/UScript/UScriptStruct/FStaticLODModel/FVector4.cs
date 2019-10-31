using System.IO;

namespace PakReader
{
    public struct FVector4
    {
        public float X, Y, Z, W;

        public FVector4(BinaryReader reader)
        {
            X = reader.ReadSingle();
            Y = reader.ReadSingle();
            Z = reader.ReadSingle();
            W = reader.ReadSingle();
        }
    }
}
