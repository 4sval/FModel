using System.IO;

namespace FModel.PakReader.IO
{
    public struct FExportBundleEntry
    {
        public const int SIZE = 8;
        
        public uint LocalExportIndex;
        public EExportCommandType CommandType;

        public FExportBundleEntry(BinaryReader reader)
        {
            LocalExportIndex = reader.ReadUInt32();
            CommandType = (EExportCommandType) reader.ReadUInt32();
        }
    }
    
    public enum EExportCommandType
    {
        ExportCommandType_Create,
        ExportCommandType_Serialize,
        ExportCommandType_Count
    };
}