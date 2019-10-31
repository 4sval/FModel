using System.IO;

namespace PakReader
{
    public struct FCompressedSegment
    {
        public int start_frame;
        public int num_frames;
        public int byte_stream_offset;
        public byte translation_compression_format;
        public byte rotation_compression_format;
        public byte scale_compression_format;

        public FCompressedSegment(BinaryReader reader)
        {
            start_frame = reader.ReadInt32();
            num_frames = reader.ReadInt32();
            byte_stream_offset = reader.ReadInt32();
            translation_compression_format = reader.ReadByte();
            rotation_compression_format = reader.ReadByte();
            scale_compression_format = reader.ReadByte();
        }
    }
}
