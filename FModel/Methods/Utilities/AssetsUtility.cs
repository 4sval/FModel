using PakReader;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FModel.Methods.Utilities
{
    class AssetsUtility
    {
        public static string GetSelectedAssetPath()
        {
            return FWindow.FCurrentAssetParentPath + "/" + FWindow.FCurrentAsset;
        }

        public static PakReader.PakReader GetPakReader()
        {
            string path = GetSelectedAssetPath();
            return AssetEntries.AssetEntriesDict
                    .Where(x => string.Equals(x.Key.Name, Path.HasExtension(path) ? path : path + ".uasset"))
                    .Select(x => x.Value).FirstOrDefault();
        }

        /// <summary>
        /// catching the uasset uexp ubulk from the reader
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static IEnumerable<FPakEntry> GetPakEntries(PakReader.PakReader reader)
        {
            string path = GetSelectedAssetPath();
            return reader.FileInfos
                .Where(x => x.Name.Contains(Path.HasExtension(path) ? path : path + "."))
                .Select(x => x);
        }

        public static AssetReader GetAssetReader(List<Stream> AssetStreamList)
        {
            if (AssetStreamList.Any() && AssetStreamList.Count == 2)
            {
                return new AssetReader(AssetStreamList[0], AssetStreamList[1]); //UASSET -> UEXP
            }
            else if (AssetStreamList.Any() && AssetStreamList.Count == 3)
            {
                return new AssetReader(AssetStreamList[0], AssetStreamList[2], AssetStreamList[1]); //UASSET -> UBULK -> UEXP
            }
            else { return null; }
        }
    }
}
