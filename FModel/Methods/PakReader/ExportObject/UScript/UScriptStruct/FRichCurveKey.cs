using System.IO;

namespace PakReader
{
    internal struct FRichCurveKey
    {
        public byte interp_mode;
        public byte tangent_mode;
        public byte tangent_weight_mode;
        public float time;
        public float arrive_tangent;
        public float arrive_tangent_weight;
        public float leave_tangent;
        public float leave_tangent_weight;

        public FRichCurveKey(BinaryReader reader)
        {
            interp_mode = reader.ReadByte();
            tangent_mode = reader.ReadByte();
            tangent_weight_mode = reader.ReadByte();
            time = reader.ReadSingle();
            arrive_tangent = reader.ReadSingle();
            arrive_tangent_weight = reader.ReadSingle();
            leave_tangent = reader.ReadSingle();
            leave_tangent_weight = reader.ReadSingle();
        }
    }
}
