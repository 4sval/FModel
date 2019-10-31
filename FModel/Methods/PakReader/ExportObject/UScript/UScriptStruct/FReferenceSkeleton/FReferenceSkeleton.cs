using System.IO;
using static PakReader.AssetReader;

namespace PakReader
{
    public struct FReferenceSkeleton
    {
        public FMeshBoneInfo[] ref_bone_info;
        public FTransform[] ref_bone_pose;
        public (string, int)[] name_to_index;

        internal FReferenceSkeleton(BinaryReader reader, FNameEntrySerialized[] name_map)
        {
            ref_bone_info = reader.ReadTArray(() => new FMeshBoneInfo(reader, name_map));
            ref_bone_pose = reader.ReadTArray(() => new FTransform(reader));

            name_to_index = new (string, int)[reader.ReadUInt32()];
            for (int i = 0; i < name_to_index.Length; i++)
            {
                name_to_index[i] = (read_fname(reader, name_map), reader.ReadInt32());
            }
        }
    }
}
