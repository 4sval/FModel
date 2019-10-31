using System.IO;
using static PakReader.AssetReader;

namespace PakReader
{
    public struct FSkeletalMaterial
    {
        public FPackageIndex Material;
        public string MaterialSlotName;
        public FMeshUVChannelInfo UVChannelData;

        internal FSkeletalMaterial(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map)
        {
            Material = new FPackageIndex(reader, import_map);

            MaterialSlotName = read_fname(reader, name_map);
            bool bSerializeImportedMaterialSlotName = reader.ReadUInt32() != 0;
            if (bSerializeImportedMaterialSlotName)
            {
                var ImportedMaterialSlotName = read_fname(reader, name_map);
            }
            UVChannelData = new FMeshUVChannelInfo(reader);
        }
    }
}
