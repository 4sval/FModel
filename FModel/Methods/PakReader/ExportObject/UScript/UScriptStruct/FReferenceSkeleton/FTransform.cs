using System.IO;

namespace PakReader
{
    public struct FTransform
    {
        public FQuat rotation;
        public FVector translation;
        public FVector scale_3d;

        internal FTransform(BinaryReader reader)
        {
            rotation = new FQuat(reader);
            translation = new FVector(reader);
            scale_3d = new FVector(reader);
        }
    }
}
