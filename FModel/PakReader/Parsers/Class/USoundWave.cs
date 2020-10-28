using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FModel.PakReader.IO;
using FModel.PakReader.Parsers.Objects;
using FModel.PakReader.Parsers.PropertyTagData;

namespace FModel.PakReader.Parsers.Class
{
    public sealed class USoundWave : UObject
    {
        public bool bCooked;
        /** Whether this sound can be streamed to avoid increased memory usage. If using Stream Caching, use Loading Behavior instead to control memory usage. */
        public bool bStreaming;
        /** Uncompressed wav data 16 bit in mono or stereo - stereo not allowed for multichannel data */
        public FByteBulkData RawData;
        /** GUID used to uniquely identify this node so it can be found in the DDC */
        public FGuid CompressedDataGuid;
        /** Set to true for programmatically generated audio. */
        public FFormatContainer[] CompressedFormatData;
        /** Format in which audio chunks are stored. */
        public FName AudioFormat;
        /** audio data. */
        public FStreamedAudioChunk[] Chunks;

        byte[] sound;
        public byte[] Sound
        {
            get
            {
                if (sound == null)
                {
                    if (!this.bStreaming)
                    {
                        if (this.bCooked && this.CompressedFormatData.Length > 0)
                            sound = this.CompressedFormatData[0].Data.Data;
                        else if (this.RawData.Data != null)
                            sound = this.RawData.Data;
                    }
                    else if (this.bStreaming && this.Chunks != null)
                    {
                        sound = new byte[this.Chunks.Sum(x => x.AudioDataSize)];
                        int offset = 0;
                        for (int i = 0; i < this.Chunks.Length; i++)
                        {
                            Buffer.BlockCopy(this.Chunks[i].BulkData.Data, 0, sound, offset, this.Chunks[i].AudioDataSize);
                            offset += this.Chunks[i].AudioDataSize;
                        }
                    }
                }
                return sound;
            }
        }

        internal USoundWave(IoPackageReader reader, Dictionary<int, PropertyInfo> properties, Stream ubulk,
            long ubulkOffset) : base(reader, properties)
        {
            Serialize(reader, ubulk, ubulkOffset);
        }

        internal USoundWave(PackageReader reader, Stream ubulk, long ubulkOffset) : base(reader)
        {
            Serialize(reader, ubulk, ubulkOffset);
        }

        private void Serialize(PackageReader reader, Stream ubulk, long ubulkOffset)
        {
            // if UE4.25+ && Windows -> True
            bStreaming = FModel.Globals.Game.Version >= EPakVersion.PATH_HASH_INDEX;

            bCooked = reader.ReadInt32() != 0;
            if (this.TryGetValue("bStreaming", out var v) && v is BoolProperty b)
                bStreaming = b.Value;

            if (!bStreaming)
            {
                if (bCooked)
                {
                    CompressedFormatData = new FFormatContainer[reader.ReadInt32()];
                    for (int i = 0; i < CompressedFormatData.Length; i++)
                    {
                        CompressedFormatData[i] = new FFormatContainer(reader, ubulk, ubulkOffset);
                    }
                    AudioFormat = CompressedFormatData[^1].FormatName;
                    CompressedDataGuid = new FGuid(reader);
                }
                else
                {
                    RawData = new FByteBulkData(reader, ubulk, ubulkOffset);
                    CompressedDataGuid = new FGuid(reader);
                }
            }
            else
            {
                CompressedDataGuid = new FGuid(reader);
                Chunks = new FStreamedAudioChunk[reader.ReadInt32()];
                AudioFormat = reader.ReadFName();
                for (int i = 0; i < Chunks.Length; i++)
                {
                    Chunks[i] = new FStreamedAudioChunk(reader, ubulk, ubulkOffset);
                }
            }
        }
    }
}
