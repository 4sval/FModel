using System.IO;

namespace PakReader
{
    internal struct FStripDataFlags
    {
        byte global_strip_flags;
        byte class_strip_flags;

        public bool editor_data_stripped => (global_strip_flags & 1) != 0;
        public bool server_data_stripped => (global_strip_flags & 2) != 0;
        public bool class_data_stripped(byte flag) => (class_strip_flags & flag) != 0;

        internal FStripDataFlags(BinaryReader reader)
        {
            global_strip_flags = reader.ReadByte();
            class_strip_flags = reader.ReadByte();
        }
    }
}
