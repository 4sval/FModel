using System.IO;

namespace PakReader
{
    public struct FStaticMeshUVItem4
    {
        public FPackedNormal[] Normal;
        public FMeshUVFloat[] UV;

        public void SerializeTangents(BinaryReader reader, bool useHighPrecisionTangents)
        {
            Normal = new FPackedNormal[3];
            if (!useHighPrecisionTangents)
            {
                Normal[0] = new FPackedNormal(reader);
                Normal[2] = new FPackedNormal(reader);
            }
            else
            {
                FPackedRGBA16N Normal, Tangent;
                Normal = new FPackedRGBA16N(reader);
                Tangent = new FPackedRGBA16N(reader);
                this.Normal[0] = Normal.ToPackedNormal();
                this.Normal[2] = Tangent.ToPackedNormal();
            }
        }

        public void SerializeTexcoords(BinaryReader reader, int uvSets, bool useStaticFloatUVs)
        {
            UV = new FMeshUVFloat[8];
            if (useStaticFloatUVs)
            {
                for (int i = 0; i < uvSets; i++)
                    UV[i] = new FMeshUVFloat(reader);
            }
            else
            {
                for (int i = 0; i < uvSets; i++)
                {
                    UV[i] = (FMeshUVFloat)new FMeshUVHalf(reader);
                }
            }
        }
    }
}
