using System.IO;

namespace PakReader
{
    internal struct FSimpleCurveKey
    {
        public float time;
        public float value;

        public FSimpleCurveKey(BinaryReader reader)
        {
            time = reader.ReadSingle();
            value = reader.ReadSingle();
        }
    }
}
