using System.IO;

namespace PakReader
{
    public struct FStaticMeshVertexBuffer
    {
        public int num_tex_coords;
        public int num_vertices;
        bool full_precision_uvs;
        bool high_precision_tangent_basis;
        public FStaticMeshUVItem4[] uv;
        //public FStaticMeshVertexDataUV? uvs;

        public FStaticMeshVertexBuffer(BinaryReader reader)
        {
            high_precision_tangent_basis = false;

            var flags = new FStripDataFlags(reader);

            num_tex_coords = reader.ReadInt32();
            num_vertices = reader.ReadInt32();
            full_precision_uvs = reader.ReadInt32() != 0;
            high_precision_tangent_basis = reader.ReadInt32() != 0;

            if (!flags.server_data_stripped)
            {
                int ItemSize, ItemCount;
                uv = new FStaticMeshUVItem4[num_vertices];

                // Tangents
                ItemSize = reader.ReadInt32();
                ItemCount = reader.ReadInt32();
                if (ItemCount != num_vertices)
                {
                    throw new FileLoadException("Invalid item count/num_vertices at pos " + reader.BaseStream.Position);
                }
                var pos = reader.BaseStream.Position;
                for (int i = 0; i < num_vertices; i++)
                {
                    uv[i].SerializeTangents(reader, high_precision_tangent_basis);
                }
                if (reader.BaseStream.Position - pos != ItemCount * ItemSize)
                {
                    throw new FileLoadException("Didn't read static mesh uvs correctly at pos " + reader.BaseStream.Position);
                }

                // Texture coordinates
                ItemSize = reader.ReadInt32();
                ItemCount = reader.ReadInt32();
                if (ItemCount != num_vertices * num_tex_coords)
                {
                    throw new FileLoadException("Invalid item count/num_vertices at pos " + reader.BaseStream.Position);
                }
                pos = reader.BaseStream.Position;
                for (int i = 0; i < num_vertices; i++)
                {
                    uv[i].SerializeTexcoords(reader, num_tex_coords, full_precision_uvs);
                }
                if (reader.BaseStream.Position - pos != ItemCount * ItemSize)
                {
                    throw new FileLoadException("Didn't read static mesh texcoords correctly at pos " + reader.BaseStream.Position);
                }
            }
            else
            {
                uv = null;
            }
        }
    }
}
