using System.IO;

namespace PakReader
{
    public struct FApexClothPhysToRenderVertData
    {
        public FVector4 PositionBaryCoordsAndDist;
        public FVector4 NormalBaryCoordsAndDist;
        public FVector4 TangentBaryCoordsAndDist;
        public short[] SimulMeshVertIndices;
        public int[] Padding;

        public FApexClothPhysToRenderVertData(BinaryReader reader)
        {
            PositionBaryCoordsAndDist = new FVector4(reader);
            NormalBaryCoordsAndDist = new FVector4(reader);
            TangentBaryCoordsAndDist = new FVector4(reader);
            SimulMeshVertIndices = new short[] { reader.ReadInt16(), reader.ReadInt16(), reader.ReadInt16(), reader.ReadInt16() };
            Padding = new int[] { reader.ReadInt32(), reader.ReadInt32() };
        }
    }
}
