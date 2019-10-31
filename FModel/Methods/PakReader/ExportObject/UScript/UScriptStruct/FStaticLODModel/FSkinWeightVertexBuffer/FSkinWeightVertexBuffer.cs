using System.IO;

namespace PakReader
{
    public struct FSkinWeightVertexBuffer
    {
        public FSkinWeightInfo[] weights;

        public FSkinWeightVertexBuffer(BinaryReader reader)
        {
            var flags = new FStripDataFlags(reader);

            var bExtraBoneInfluences = reader.ReadInt32() != 0;
            var num_vertices = reader.ReadInt32();

            if (flags.server_data_stripped)
            {
                weights = null;
                return;
            }

            var _element_size = reader.ReadInt32();
            var num_influences = bExtraBoneInfluences ? 8 : 4;
            weights = reader.ReadTArray(() => new FSkinWeightInfo(reader, num_influences));
        }
    }
}
