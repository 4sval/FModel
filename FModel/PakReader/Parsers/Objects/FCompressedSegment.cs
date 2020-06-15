namespace PakReader.Parsers.Objects
{
    public readonly struct FCompressedSegment : IUStruct
    {
        public readonly int StartFrame;
        public readonly int NumFrames;
        public readonly int ByteStreamOffset;
        public readonly EAnimationCompressionFormat TranslationCompressionFormat;
        public readonly EAnimationCompressionFormat RotationCompressionFormat;
        public readonly EAnimationCompressionFormat ScaleCompressionFormat;

        internal FCompressedSegment(PackageReader reader)
        {
            StartFrame = reader.ReadInt32();
            NumFrames = reader.ReadInt32();
            ByteStreamOffset = reader.ReadInt32();
            TranslationCompressionFormat = (EAnimationCompressionFormat)reader.ReadByte();
            RotationCompressionFormat = (EAnimationCompressionFormat)reader.ReadByte();
            ScaleCompressionFormat = (EAnimationCompressionFormat)reader.ReadByte();
        }
    }
}
