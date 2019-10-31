using System.IO;

namespace PakReader
{
    public struct FSkelMeshSection
    {
        public ushort material_index;
        public uint base_index;
        public uint num_triangles;
        public uint base_vertex_index;
        public FApexClothPhysToRenderVertData[] cloth_mapping_data;
        public ushort[] bone_map;
        public int num_vertices;
        public int max_bone_influences;
        public FClothingSectionData clothing_data;
        public bool disabled;

        internal FSkelMeshSection(BinaryReader reader, FNameEntrySerialized[] name_map)
        {
            var flags = new FStripDataFlags(reader);
            material_index = reader.ReadUInt16();
            base_index = reader.ReadUInt32();
            num_triangles = reader.ReadUInt32();

            var _recompute_tangent = reader.ReadUInt32() != 0;
            var _cast_shadow = reader.ReadUInt32() != 0;
            base_vertex_index = reader.ReadUInt32();
            cloth_mapping_data = reader.ReadTArray(() => new FApexClothPhysToRenderVertData(reader));
            bool HasClothData = cloth_mapping_data.Length > 0;

            bone_map = reader.ReadTArray(() => reader.ReadUInt16());
            num_vertices = reader.ReadInt32();
            max_bone_influences = reader.ReadInt32();
            var _correspond_cloth_asset_index = reader.ReadInt16();
            clothing_data = new FClothingSectionData(reader);
            var _vertex_buffer = new FDuplicatedVerticesBuffer(reader);
            disabled = reader.ReadUInt32() != 0;
        }
    }
}
