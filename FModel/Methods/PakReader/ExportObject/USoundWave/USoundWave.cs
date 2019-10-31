using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static PakReader.AssetReader;

namespace PakReader
{
    public sealed class USoundWave : ExportObject, IDisposable
    {
        [JsonIgnore]
        public UObject base_object;
        [JsonIgnore]
        public bool bStreaming;
        [JsonIgnore]
        public bool bCooked;
        [JsonIgnore]
        public FByteBulkData rawData;
        [JsonIgnore]
        public FGuid compressedDataGUID;
        [JsonIgnore]
        public List<FSoundFormatData> compressedFormatData;
        public string format;
        [JsonIgnore]
        public List<FStreamedAudioChunk> streamedAudioChunks;
        [JsonIgnore]
        public int sampleRate;
        [JsonIgnore]
        public int numChannels;

        internal USoundWave(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map, int asset_file_size, long export_size, BinaryReader ubulk)
        {
            base_object = new UObject(reader, name_map, import_map, "SoundWave", true);

            bCooked = reader.ReadInt32() == 1;
            bStreaming = false;
            FPropertyTag streamingProperty = base_object.properties.Where(x => x.name == "bStreaming").Select(x => x).FirstOrDefault();
            if (streamingProperty.name != null)
            {
                if (streamingProperty.tag == FPropertyTagType.BoolProperty)
                {
                    bStreaming = (bool)streamingProperty.tag_data;
                }
            }
            if (!bStreaming)
            {
                if (bCooked)
                {
                    compressedFormatData = new List<FSoundFormatData>();
                    int elemCount = reader.ReadInt32();
                    for (int i = 0; i < elemCount; i++)
                    {
                        compressedFormatData.Add(new FSoundFormatData(reader, name_map, asset_file_size, export_size, ubulk));
                        format = compressedFormatData[i].name;
                    }
                    compressedDataGUID = new FGuid(reader);
                }
                else
                {
                    rawData = new FByteBulkData(reader, ubulk, export_size + asset_file_size);
                    compressedDataGUID = new FGuid(reader);
                }
            }
            else
            {
                compressedDataGUID = new FGuid(reader);
                int numChunks = reader.ReadInt32();
                format = read_fname(reader, name_map);
                streamedAudioChunks = new List<FStreamedAudioChunk>();
                for (int i = 0; i < numChunks; i++)
                {
                    streamedAudioChunks.Add(new FStreamedAudioChunk(reader, asset_file_size, export_size, ubulk));
                }
            }
        }

        public void Dispose()
        {
            base_object = null;
            rawData = new FByteBulkData();
            compressedDataGUID = new FGuid();
            compressedFormatData = null;
            streamedAudioChunks = null;
        }
    }
}
