using System.IO;

namespace PakReader.Parsers.Objects
{
    public readonly struct FSampleChunk : IUStruct
    {
        public readonly uint Manufacturer;
        public readonly uint Product;
        public readonly uint SamplePeriod;
        public readonly uint MidiUnityNote;
        public readonly uint MidiPitchFraction;
        public readonly uint SmpteFormat;
        public readonly uint SmpteOffset;
        public readonly uint SampleLoops;
        public readonly uint SamplerData;
        public readonly FSampleLoop[] SampleLoop;

        internal FSampleChunk(BinaryReader reader)
        {
            Manufacturer = reader.ReadUInt32();
            Product = reader.ReadUInt32();
            SamplePeriod = reader.ReadUInt32();
            MidiUnityNote = reader.ReadUInt32();
            MidiPitchFraction = reader.ReadUInt32();
            SmpteFormat = reader.ReadUInt32();
            SmpteOffset = reader.ReadUInt32();
            SampleLoops = reader.ReadUInt32();
            SamplerData = reader.ReadUInt32();
            SampleLoop = new FSampleLoop[2]
            {
                new FSampleLoop(reader),
                new FSampleLoop(reader)
            };
        }
    }
}
