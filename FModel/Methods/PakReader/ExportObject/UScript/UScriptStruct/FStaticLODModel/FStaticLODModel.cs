using System.IO;

namespace PakReader
{
    public struct FStaticLODModel
    {
        public FSkelMeshSection[] Sections;
        public FMultisizeIndexContainer Indices;
        public FMultisizeIndexContainer AdjacencyIndexBuffer;
        public short[] ActiveBoneIndices;
        public short[] RequiredBones;
        //public FSkelMeshChunk Chunks;
        //public int Size;
        public int NumVertices;
        public int NumTexCoords;
        //public FIntBulkData RawPointIndices;
        //public int[] MeshToImportVertexMap;
        //public int MaxImportVertex;
        public FSkeletalMeshVertexBuffer VertexBufferGPUSkin;
        //public FSkeletalMeshVertexClothBuffer ColorVertexBuffer;
        public FSkeletalMeshVertexClothBuffer ClothVertexBuffer;
        public FSkinWeightProfilesData SkinWeightProfilesData;

        internal FStaticLODModel(BinaryReader reader, FNameEntrySerialized[] name_map, bool has_vertex_colors)
        {
            var flags = new FStripDataFlags(reader);
            Sections = reader.ReadTArray(() => new FSkelMeshSection(reader, name_map));
            Indices = new FMultisizeIndexContainer(reader);
            ActiveBoneIndices = reader.ReadTArray(() => reader.ReadInt16());
            RequiredBones = reader.ReadTArray(() => reader.ReadInt16());

            if (flags.server_data_stripped || flags.class_data_stripped(2))
            {
                throw new FileLoadException("Could not read FSkelMesh, no renderable data");
            }

            var position_vertex_buffer = new FPositionVertexBuffer(reader);
            var static_mesh_vertex_buffer = new FStaticMeshVertexBuffer(reader);
            var skin_weight_vertex_buffer = new FSkinWeightVertexBuffer(reader);

            if (has_vertex_colors)
            {
                var colour_vertex_buffer = new FColorVertexBuffer(reader);
            }

            AdjacencyIndexBuffer = default;
            if (!flags.class_data_stripped(1))
            {
                AdjacencyIndexBuffer = new FMultisizeIndexContainer(reader);
            }

            ClothVertexBuffer = default;
            if (HasClothData(Sections))
            {
                ClothVertexBuffer = new FSkeletalMeshVertexClothBuffer(reader);
            }

            SkinWeightProfilesData = new FSkinWeightProfilesData(reader, name_map);

            VertexBufferGPUSkin = new FSkeletalMeshVertexBuffer();
            VertexBufferGPUSkin.bUseFullPrecisionUVs = true;
            NumVertices = position_vertex_buffer.num_verts;
            NumTexCoords = static_mesh_vertex_buffer.num_tex_coords;

            VertexBufferGPUSkin.VertsFloat = new FGPUVert4Float[NumVertices];
            for (int i = 0; i < NumVertices; i++)
            {
                var V = new FGPUVert4Float();
                var SV = static_mesh_vertex_buffer.uv[i];
                V.Pos = position_vertex_buffer.verts[i];
                V.Infs = skin_weight_vertex_buffer.weights[i];
                V.Normal = SV.Normal; // i mean, we're not using it for anything else, are we?
                V.UV = SV.UV;
                VertexBufferGPUSkin.VertsFloat[i] = V;
            }
        }

        static bool HasClothData(FSkelMeshSection[] sections)
        {
            foreach (var s in sections)
                if (s.cloth_mapping_data.Length > 0)
                    return true;
            return false;
        }
    }
}
