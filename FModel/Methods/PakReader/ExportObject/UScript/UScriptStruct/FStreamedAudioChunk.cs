using System.IO;

namespace PakReader
{
    public class FStreamedAudioChunk
    {
        public FByteBulkData data;
        public int dataSize;
        public int audioDataSize;

        internal FStreamedAudioChunk(BinaryReader reader, int asset_file_size, long export_size, BinaryReader ubulk)
        {
            bool bCooked = reader.ReadInt32() == 1;
            if (bCooked)
            {
                data = new FByteBulkData(reader, ubulk, export_size + asset_file_size);
                dataSize = reader.ReadInt32();
                audioDataSize = reader.ReadInt32();
            }
            else
            {
                reader.BaseStream.Position -= 4;
                throw new FileLoadException("StreamedAudioChunks must be cooked");
            }
        }
    }
}
