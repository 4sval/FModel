using System.IO;

namespace PakReader.Parsers.Objects
{
    public readonly struct FRotator : IUStruct
    {
        public readonly float Pitch;
        public readonly float Yaw;
        public readonly float Roll;

        internal FRotator(BinaryReader reader)
        {
            Pitch = reader.ReadSingle();
            Yaw = reader.ReadSingle();
            Roll = reader.ReadSingle();
        }
    }
}
