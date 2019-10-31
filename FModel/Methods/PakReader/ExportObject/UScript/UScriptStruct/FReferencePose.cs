using System.IO;
using static PakReader.AssetReader;

namespace PakReader
{
    public struct FReferencePose
    {
        public string pose_name;
        public FTransform[] reference_pose;

        internal FReferencePose(BinaryReader reader, FNameEntrySerialized[] name_map)
        {
            pose_name = read_fname(reader, name_map);
            reference_pose = reader.ReadTArray(() => new FTransform(reader));
        }
    }
}
