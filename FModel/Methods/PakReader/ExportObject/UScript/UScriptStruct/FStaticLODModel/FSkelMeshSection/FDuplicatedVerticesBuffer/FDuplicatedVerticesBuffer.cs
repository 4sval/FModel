using System.IO;

namespace PakReader
{
    public struct FDuplicatedVerticesBuffer
    {
        public int[] dup_vert;
        public FIndexLengthPair[] dup_vert_index;

        public FDuplicatedVerticesBuffer(BinaryReader reader)
        {
            dup_vert = reader.ReadTArray(() => reader.ReadInt32());
            dup_vert_index = reader.ReadTArray(() => new FIndexLengthPair(reader));
        }
    }
}
