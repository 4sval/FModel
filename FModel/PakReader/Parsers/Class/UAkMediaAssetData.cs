using PakReader.Parsers.Objects;
using System;
using System.IO;
using System.Linq;

namespace PakReader.Parsers.Class
{
    public sealed class UAkMediaAssetData : UObject
    {
        public FAkMediaDataChunk[] DataChunks { get; }

        byte[] sound;
        public byte[] Sound
        {
            get
            {
                if (sound == null)
                {
                    sound = new byte[this.DataChunks.Sum(x => x.Data.Data.Length)];
                    int offset = 0;
                    for (int i = 0; i < this.DataChunks.Length; i++)
                    {
                        Buffer.BlockCopy(this.DataChunks[i].Data.Data, 0, sound, offset, this.DataChunks[i].Data.Data.Length);
                        offset += this.DataChunks[i].Data.Data.Length;
                    }
                }
                return sound;
            }
        }

        internal UAkMediaAssetData(PackageReader reader, Stream ubulk, long bulkOffset) : base(reader)
        {
            DataChunks = new FAkMediaDataChunk[reader.ReadInt32()];
            for (int i = 0; i < DataChunks.Length; ++i)
            {
                DataChunks[i] = new FAkMediaDataChunk(reader, ubulk, bulkOffset);
            }
        }
    }
}
