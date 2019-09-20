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

        public static AssetReader GetAssetReader(Stream[] AssetStreamList)
        {
            if (AssetStreamList[0] != null && AssetStreamList.Length >= 2 && AssetStreamList.Length <= 3)
            {
                return new AssetReader(AssetStreamList[0], AssetStreamList[1], AssetStreamList[2] != null ? AssetStreamList[2]: null); //UASSET -> UEXP -> UBULK IF EXIST
            }
            else { return null; }
        }
    }
}
