using System.IO;

namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct FVector4 : IUStruct
    {
        /** The vector's X-component. */
        public readonly float X;
        /** The vector's Y-component. */
        public readonly float Y;
        /** The vector's Z-component. */
        public readonly float Z;
        /** The vector's W-component. */
        public readonly float W;

        internal FVector4(BinaryReader reader)
        {
            X = reader.ReadSingle();
            Y = reader.ReadSingle();
            Z = reader.ReadSingle();
            W = reader.ReadSingle();
        }
    }
}
