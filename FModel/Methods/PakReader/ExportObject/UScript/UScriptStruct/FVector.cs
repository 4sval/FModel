using System;
using System.IO;

namespace PakReader
{
    public struct FVector : IEquatable<FVector>
    {
        public float X;
        public float Y;
        public float Z;

        internal FVector(BinaryReader reader)
        {
            X = reader.ReadSingle();
            Y = reader.ReadSingle();
            Z = reader.ReadSingle();
        }

        public static FVector operator -(FVector a, FVector b)
        {
            return new FVector
            {
                X = a.X - b.X,
                Y = a.Y - b.Y,
                Z = a.Z - b.Z
            };
        }

        public static FVector operator +(FVector a, FVector b)
        {
            return new FVector
            {
                X = a.X + b.X,
                Y = a.Y + b.Y,
                Z = a.Z + b.Z
            };
        }

        public bool Equals(FVector other) => other.X == X && other.Y == Y && other.Z == Z;

        //public static bool operator ==(FVector a, FVector b) => a.Equals(b);

        //public static bool operator !=(FVector a, FVector b) => !a.Equals(b);

        public static implicit operator CVec3(FVector me) => new CVec3
        {
            v = new float[] { me.X, me.Y, me.Z }
        };

        public static implicit operator FVector(CVec3 me) => new FVector
        {
            X = me.v[0],
            Y = me.v[1],
            Z = me.v[2]
        };

        public static implicit operator CVec4(FVector me) => new CVec4
        {
            v = new float[] { me.X, me.Y, me.Z, 0 }
        };

        public static implicit operator FVector(FPackedNormal V) => new FVector
        {
            X = ((V.Data & 0xFF) / 127.5f) - 1,
            Y = ((V.Data >> 8 & 0xFF) / 127.5f) - 1,
            Z = ((V.Data >> 16 & 0xFF) / 127.5f) - 1
        };

        public void Write(BinaryWriter writer)
        {
            writer.Write(X);
            writer.Write(Y);
            writer.Write(Z);
        }
    }
}
