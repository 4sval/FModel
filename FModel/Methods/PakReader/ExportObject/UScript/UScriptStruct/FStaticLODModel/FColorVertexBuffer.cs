using System.IO;

namespace PakReader
{
    public struct FColorVertexBuffer
    {
        public int stride;
        public int num_verts;
        public FColor[] colors;

        public FColorVertexBuffer(BinaryReader reader)
        {
            var flags = new FStripDataFlags(reader);
            stride = reader.ReadInt32();
            num_verts = reader.ReadInt32();
            colors = null;
            if (!flags.server_data_stripped && num_verts > 0)
            {
                var _element_size = reader.ReadInt32();
                colors = reader.ReadTArray(() => new FColor(reader));
            }
        }
    }
}
