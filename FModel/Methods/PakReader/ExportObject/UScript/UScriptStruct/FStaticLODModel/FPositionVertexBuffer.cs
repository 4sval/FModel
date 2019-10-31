using System.IO;

namespace PakReader
{
    public struct FPositionVertexBuffer
    {
        public FVector[] verts;
        public int stride;
        public int num_verts;

        public FPositionVertexBuffer(BinaryReader reader)
        {
            stride = reader.ReadInt32();
            num_verts = reader.ReadInt32();
            var _element_size = reader.ReadInt32();
            verts = reader.ReadTArray(() => new FVector(reader));
        }
    }
}
