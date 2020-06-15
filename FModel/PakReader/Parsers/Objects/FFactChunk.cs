using System.IO;

namespace PakReader.Parsers.Objects
{
    public readonly struct FFactChunk : IUStruct
    {
        public readonly uint TotalSamples;                      // total samples per channel
        public readonly uint DelaySamplesInputOverlap;          // samples of input and overlap delay
        public readonly uint DelaySamplesInputOverlapEncoder;   // samples of input and overlap and encoder delay

        internal FFactChunk(BinaryReader reader)
        {
            TotalSamples = reader.ReadUInt32();
            DelaySamplesInputOverlap = reader.ReadUInt32();
            DelaySamplesInputOverlapEncoder = reader.ReadUInt32();
        }
    }
}
