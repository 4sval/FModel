using System.IO;

namespace PakReader
{
    public struct FClothingSectionData
    {
        public FGuid asset_guid;
        public int asset_lod_index;

        public FClothingSectionData(BinaryReader reader)
        {
            asset_guid = new FGuid(reader);
            asset_lod_index = reader.ReadInt32();
        }
    }
}
