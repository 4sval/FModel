using System.IO;

namespace PakReader
{
    public struct FBoxSphereBounds
    {
        public FVector origin;
        public FVector box_extend;
        public float sphere_radius;

        internal FBoxSphereBounds(BinaryReader reader)
        {
            origin = new FVector(reader);
            box_extend = new FVector(reader);
            sphere_radius = reader.ReadSingle();
        }
    }
}
