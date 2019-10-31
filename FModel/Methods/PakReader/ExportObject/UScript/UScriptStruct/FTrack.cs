namespace PakReader
{
    public struct FTrack
    {
        public FVector[] translation;
        public FQuat[] rotation;
        public FVector[] scale;
        public float[] translation_times;
        public float[] rotation_times;
        public float[] scale_times;
    }
}
