using System;
using System.IO;

namespace PakReader
{
    public struct FQuat
    {
        public float X;
        public float Y;
        public float Z;
        public float W;

        internal FQuat(BinaryReader reader)
        {
            X = reader.ReadSingle();
            Y = reader.ReadSingle();
            Z = reader.ReadSingle();
            W = reader.ReadSingle();
        }

        public void rebuild_w()
        {
            var ww = 1f - (X * X + Y * Y + Z * Z);
            W = ww > 0 ? (float)Math.Sqrt(ww) : 0;
        }

        public static implicit operator CQuat(FQuat me) => new CQuat
        {
            x = me.X,
            y = me.Y,
            z = me.Z,
            w = me.W
        };

        public static implicit operator FQuat(CQuat me) => new FQuat
        {
            X = me.x,
            Y = me.y,
            Z = me.z,
            W = me.w
        };

        public void Write(BinaryWriter writer)
        {
            writer.Write(X);
            writer.Write(Y);
            writer.Write(Z);
            writer.Write(W);
        }
    }
}
