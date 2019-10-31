using System.IO;
using static PakReader.AssetReader;

namespace PakReader
{
    public struct FGameplayTagContainer
    {
        public string[] gameplay_tags;

        internal FGameplayTagContainer(BinaryReader reader, FNameEntrySerialized[] name_map)
        {
            uint length = reader.ReadUInt32();
            gameplay_tags = new string[length];

            for (int i = 0; i < length; i++)
            {
                gameplay_tags[i] = read_fname(reader, name_map);
            }
        }
    }
}
