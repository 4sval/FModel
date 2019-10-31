using System.IO;
using static PakReader.AssetReader;

namespace PakReader
{
    public sealed class USkeleton : ExportObject
    {
        public UObject super_object;
        public FReferenceSkeleton reference_skeleton;
        public (string, FReferencePose)[] anim_retarget_sources;

        internal USkeleton(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map)
        {
            super_object = new UObject(reader, name_map, import_map, "Skeleton", true);
            reference_skeleton = new FReferenceSkeleton(reader, name_map);

            anim_retarget_sources = new (string, FReferencePose)[reader.ReadUInt32()];
            for (int i = 0; i < anim_retarget_sources.Length; i++)
            {
                anim_retarget_sources[i] = (read_fname(reader, name_map), new FReferencePose(reader, name_map));
            }
        }
    }
}
