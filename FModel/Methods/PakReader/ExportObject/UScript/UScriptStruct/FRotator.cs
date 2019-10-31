using System.IO;

namespace PakReader
{
    public struct FRotator
    {
        public float pitch;
        public float yaw;
        public float roll;

        internal FRotator(BinaryReader reader)
        {
            pitch = reader.ReadSingle();
            yaw = reader.ReadSingle();
            roll = reader.ReadSingle();
        }
    }
}
