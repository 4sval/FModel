using System.IO;

namespace PakReader
{
    public struct FVector2D
    {
        public float x;
        public float y;

        internal FVector2D(BinaryReader reader)
        {
            x = reader.ReadSingle();
            y = reader.ReadSingle();
        }
    }
}
