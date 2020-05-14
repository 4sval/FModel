using System.IO;

namespace PakReader.Parsers.Objects
{
    public readonly struct FStreamedAudioChunk : IUStruct
    {
        /** Size of the chunk of data in bytes including zero padding */
        public readonly int DataSize;
        /** Size of the audio data. */
        public readonly int AudioDataSize;
        /** Bulk data if stored in the package. */
        public readonly FByteBulkData BulkData;

        internal FStreamedAudioChunk(PackageReader reader, Stream ubulk, long ubulkOffset)
        {
            bool bCooked = reader.ReadInt32() != 0;
            if (bCooked)
            {
                BulkData = new FByteBulkData(reader, ubulk, ubulkOffset);
                DataSize = reader.ReadInt32();
                AudioDataSize = reader.ReadInt32();
            }
            else
            {
                reader.Position -= 4;
                throw new FileLoadException("StreamedAudioChunk must be cooked");
            }
        }
    }
}
