using System.IO;

namespace PakReader
{
    public struct FAnimKeyHeader
    {
        public AnimationCompressionFormat key_format;
        public uint component_mask;
        public uint num_keys;
        public bool has_time_tracks;

        public FAnimKeyHeader(BinaryReader reader)
        {
            var packed = reader.ReadUInt32();
            key_format = (AnimationCompressionFormat)(packed >> 28);
            component_mask = (packed >> 24) & 0xF;
            num_keys = packed & 0xFFFFFF;
            has_time_tracks = (component_mask & 8) != 0;
        }
    }

    public enum AnimationCompressionFormat : uint
    {
        None,
        Float96NoW,
        Fixed48NoW,
        IntervalFixed32NoW,
        Fixed32NoW,
        Float32NoW,
        Identity,
    }
}
