using System.IO;
using static PakReader.AssetReader;

namespace PakReader
{
    public struct FScriptDelegate
    {
        public int obj;
        public string name;

        internal FScriptDelegate(BinaryReader reader, FNameEntrySerialized[] name_map)
        {
            obj = reader.ReadInt32();
            name = read_fname(reader, name_map);
        }
    }
}
