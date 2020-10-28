using System.Collections.Generic;

namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct FAssetDataTagMapSharedView : IUStruct
    {
        public readonly SortedList<string, string> Map;

        internal FAssetDataTagMapSharedView(FNameTableArchiveReader reader)
        {
            int l = reader.Loader.ReadInt32();
            Map = new SortedList<string, string>(l);
            for (int i = 0; i < l; i++)
            {
                Map[reader.ReadFName().String] = reader.Loader.ReadFString();
            }
        }
    }
}
