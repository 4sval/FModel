using System.IO;

namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct FStripDataFlags
    {
        readonly byte GlobalStripFlags;
        readonly byte ClassStripFlags;

        public bool EditorDataStripped => (GlobalStripFlags & (byte)EStrippedData.Editor) != 0;
        public bool DataStrippedForServer => (GlobalStripFlags & (byte)EStrippedData.Server) != 0;

        public bool ClassDataStripped(byte InFlags) => (ClassStripFlags & InFlags) != 0;

        internal FStripDataFlags(BinaryReader reader)
        {
            GlobalStripFlags = reader.ReadByte();
            ClassStripFlags = reader.ReadByte();
        }

        enum EStrippedData : byte
        {
            None = 0,

            /* Editor data */
            Editor = 1,
            /* All data not required for dedicated server to work correctly (usually includes editor data). */
            Server = 2,

            // Add global flags here (up to 8 including the already defined ones).

            /** All flags */
            All = 0xff
        }
    }
}
