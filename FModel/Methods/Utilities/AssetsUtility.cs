using FModel.Methods.SyntaxHighlighter;
using Newtonsoft.Json;
using PakReader;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;

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

        public static string GetAssetJsonData(PakReader.PakReader reader, IEnumerable<FPakEntry> entriesList, bool loadImageInBox = false)
        {
            Stream[] AssetStreamArray = new Stream[3];

            foreach (FPakEntry entry in entriesList)
            {
                switch (Path.GetExtension(entry.Name.ToLowerInvariant()))
                {
                    case ".ini":
                        FWindow.FMain.Dispatcher.InvokeAsync(() =>
                        {
                            FWindow.FMain.AssetPropertiesBox_Main.SyntaxHighlighting = ResourceLoader.LoadHighlightingDefinition("Ini.xshd");
                        });
                        using (var s = reader.GetPackageStream(entry))
                        using (var r = new StreamReader(s))
                            return r.ReadToEnd();
                    case ".uproject":
                    case ".uplugin":
                    case ".upluginmanifest":
                        using (var s = reader.GetPackageStream(entry))
                        using (var r = new StreamReader(s))
                            return r.ReadToEnd();
                    case ".locmeta":
                        using (var s = reader.GetPackageStream(entry))
                            return JsonConvert.SerializeObject(new LocMetaFile(s), Formatting.Indented);
                    case ".locres":
                        using (var s = reader.GetPackageStream(entry))
                            return JsonConvert.SerializeObject(new LocResFile(s).Entries, Formatting.Indented);
                    case ".udic":
                        using (var s = reader.GetPackageStream(entry))
                        using (var r = new BinaryReader(s))
                            return JsonConvert.SerializeObject(new UDicFile(r).Header, Formatting.Indented);
                    case ".bin":
                        if (string.Equals(entry.Name, "/FortniteGame/AssetRegistry.bin") || !entry.Name.Contains("AssetRegistry")) //MEMORY ISSUE
                            break;

                        using (var s = reader.GetPackageStream(entry))
                            return JsonConvert.SerializeObject(new AssetRegistryFile(s), Formatting.Indented);
                    default:
                        if (entry.Name.EndsWith(".uasset"))
                            AssetStreamArray[0] = reader.GetPackageStream(entry);

                        if (entry.Name.EndsWith(".uexp"))
                            AssetStreamArray[1] = reader.GetPackageStream(entry);

                        if (entry.Name.EndsWith(".ubulk"))
                            AssetStreamArray[2] = reader.GetPackageStream(entry);
                        break;
                }
            }

            AssetReader ar = GetAssetReader(AssetStreamArray);
            if (ar != null)
            {
                if (loadImageInBox)
                {
                    FWindow.FMain.Dispatcher.InvokeAsync(() =>
                    {
                        FWindow.FMain.ImageBox_Main.Source = GetTexture2D(ar);
                    });
                }

                return JsonConvert.SerializeObject(ar.Exports, Formatting.Indented);
            }

            return string.Empty;
        }

        public static ImageSource GetTexture2D(AssetReader ar)
        {
            ExportObject eo = ar.Exports.Where(x => x is Texture2D).FirstOrDefault();
            if (eo != null)
            {
                SkiaSharp.SKImage image = ((Texture2D)eo).GetImage();
                if (image != null)
                {
                    using (var data = image.Encode())
                    using (var stream = data.AsStream())
                    {
                        return ImagesUtility.GetImageSource(stream);
                    }
                }
            }

            return null;
        }
    }
}
