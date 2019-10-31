using FModel.Methods.SyntaxHighlighter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PakReader;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FProp = FModel.Properties.Settings;

namespace FModel.Methods.Utilities
{
    static class AssetsUtility
    {
        public static string GetAssetPathToCopy(bool isName = false, bool withExtension = true)
        {
            string treePath = TreeViewUtility.GetFullPath(FWindow.TVItem);
            string path = treePath + "/" + FWindow.FCurrentAsset;

            path = path.Contains(".") ? path : path + ".uasset";

            if (isName) { path = Path.GetFileName(path); }

            if (!withExtension) { path = isName ? Path.GetFileNameWithoutExtension(path) : FoldersUtility.GetFullPathWithoutExtension(path); }
            if (path.StartsWith("/")) { path = path.Substring(1); }

            new UpdateMyConsole(path, CColors.Blue).Append();
            new UpdateMyConsole(" Copied!", CColors.White, true).Append();
            return path;

        }

        public static string GetReadableSize(long size)
        {
            long absolute_i = size < 0 ? -size : size;
            string suffix;
            double readable;
            if (absolute_i >= 0x40000000)
            {
                suffix = "GB";
                readable = size >> 20;
            }
            else if (absolute_i >= 0x100000)
            {
                suffix = "MB";
                readable = size >> 10;
            }
            else if (absolute_i >= 0x400)
            {
                suffix = "KB";
                readable = size;
            }
            else
            {
                return size.ToString("0 B");
            }
            readable = readable / 1024;
            return readable.ToString("0.## ") + suffix;
        }

        public static PakReader.PakReader GetPakReader(string assetPath)
        {
            string trigger = Path.HasExtension(assetPath) ? assetPath : assetPath + ".uasset";
            if (AssetEntries.AssetEntriesDict.ContainsKey(trigger))
            {
                return AssetEntries.AssetEntriesDict[trigger];
            }
            return null;
        }

        /// <summary>
        /// goes from all files in a pak -> to only the ones (uasset, uexp, ubulk) we need to loop to get an asset data
        /// it's ugly but way better than before when it's about time
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        public static List<FPakEntry> GetPakEntries(string assetPath)
        {
            if (Path.HasExtension(assetPath))
            {
                if (AssetEntries.ArraySearcher.ContainsKey(assetPath))
                {
                    return AssetEntries.ArraySearcher[assetPath].Where(x => x.Name == assetPath).Select(x => x).ToList<FPakEntry>();
                }
            }
            else
            {
                List<FPakEntry> toReturn = new List<FPakEntry>();
                if (AssetEntries.ArraySearcher.ContainsKey(assetPath + ".uasset"))
                {
                    toReturn.Add(AssetEntries.ArraySearcher[assetPath + ".uasset"].Where(x => x.Name == assetPath + ".uasset").Select(x => x).FirstOrDefault());
                }
                if (AssetEntries.ArraySearcher.ContainsKey(assetPath + ".uexp"))
                {
                    toReturn.Add(AssetEntries.ArraySearcher[assetPath + ".uexp"].Where(x => x.Name == assetPath + ".uexp").Select(x => x).FirstOrDefault());
                }
                if (AssetEntries.ArraySearcher.ContainsKey(assetPath + ".ubulk"))
                {
                    toReturn.Add(AssetEntries.ArraySearcher[assetPath + ".ubulk"].Where(x => x.Name == assetPath + ".ubulk").Select(x => x).FirstOrDefault());
                }
                return toReturn;
            }
            return null;
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
                        {
                            break;
                        }

                        using (var s = reader.GetPackageStream(entry))
                            return JsonConvert.SerializeObject(new AssetRegistryFile(s), Formatting.Indented);
                    default:
                        if (entry.Name.EndsWith(".uasset")) { AssetStreamArray[0] = reader.GetPackageStream(entry); }

                        if (entry.Name.EndsWith(".uexp")) { AssetStreamArray[1] = reader.GetPackageStream(entry); }

                        if (entry.Name.EndsWith(".ubulk")) { AssetStreamArray[2] = reader.GetPackageStream(entry); }
                        break;
                }
            }

            AssetReader ar = GetAssetReader(AssetStreamArray);
            if (ar != null)
            {
                if (loadImageInBox)
                {
                    foreach (ExportObject eo in ar.Exports)
                    {   
                        switch (eo)
                        {
                            case Texture2D texture:
                                SKImage image = texture.GetImage();
                                if (image != null)
                                {
                                    using (var data = image.Encode())
                                    using (var stream = data.AsStream())
                                    {
                                        ImageSource img = ImagesUtility.GetImageSource(stream);
                                        FWindow.FMain.Dispatcher.InvokeAsync(() =>
                                        {
                                            FWindow.FMain.ImageBox_Main.Source = BitmapFrame.Create((BitmapSource)img); //thread safe and fast af

                                            if (FWindow.FMain.MI_Auto_Save_Images.IsChecked) //auto save images
                                            {
                                                ImagesUtility.SaveImage(FProp.Default.FOutput_Path + "\\Icons\\" + FWindow.FCurrentAsset + ".png");
                                            }
                                        });
                                    }
                                }
                                return JsonConvert.SerializeObject(texture.textures, Formatting.Indented);
                            case USoundWave sound:
                                using (sound)
                                {
                                    byte[] s = readSound(sound);
                                    if (s != null)
                                    {
                                        string path = FProp.Default.FOutput_Path + "\\Sounds\\" + FWindow.FCurrentAsset + ".ogg";
                                        File.WriteAllBytes(path, s);

                                        //open sound
                                        if (FProp.Default.FOpenSounds)
                                        {
                                            FoldersUtility.OpenWithDefaultProgram(path);
                                        }
                                    }

                                    GC.Collect();
                                    GC.WaitForPendingFinalizers();
                                    return JsonConvert.SerializeObject(sound.base_object, Formatting.Indented);
                                }
                        }
                    }
                }

                return JsonConvert.SerializeObject(ar.Exports, Formatting.Indented);
            }

            return string.Empty;
        }

        public static byte[] readSound(USoundWave sound)
        {
            if (!sound.bStreaming)
            {
                if (sound.bCooked && sound.compressedFormatData.Count > 0)
                {
                    FSoundFormatData data = sound.compressedFormatData[0];
                    return data.data.data;
                }
                else if(sound.rawData.data != null)
                {
                    return sound.rawData.data;
                }
            }
            else if (sound.bStreaming && sound.streamedAudioChunks != null && !string.IsNullOrEmpty(sound.format))
            {
                List<byte> bytes = new List<byte>();
                foreach (FStreamedAudioChunk chunk in sound.streamedAudioChunks)
                {
                    chunk.data.data.ToList().ForEach(x => bytes.Add(x));
                }
                return bytes.ToArray();
            }
            return null;
        }

        public static Stream GetStreamImageFromPath(string AssetFullPath)
        {
            PakReader.PakReader reader = GetPakReader(AssetFullPath);
            if (reader != null)
            {
                List<FPakEntry> entriesList = GetPakEntries(AssetFullPath);
                Stream[] AssetStreamArray = new Stream[3];
                foreach (FPakEntry entry in entriesList)
                {
                    switch (Path.GetExtension(entry.Name.ToLowerInvariant()))
                    {
                        case ".ini":
                            break;
                        case ".uproject":
                        case ".uplugin":
                        case ".upluginmanifest":
                            break;
                        case ".locmeta":
                            break;
                        case ".locres":
                            break;
                        case ".udic":
                            break;
                        case ".bin":
                            break;
                        default:
                            if (entry.Name.EndsWith(".uasset")) { AssetStreamArray[0] = reader.GetPackageStream(entry); }

                            if (entry.Name.EndsWith(".uexp")) { AssetStreamArray[1] = reader.GetPackageStream(entry); }

                            if (entry.Name.EndsWith(".ubulk")) { AssetStreamArray[2] = reader.GetPackageStream(entry); }
                            break;
                    }
                }

                AssetReader ar = GetAssetReader(AssetStreamArray);
                if (ar != null)
                {
                    ExportObject eo = ar.Exports.Where(x => x is Texture2D).FirstOrDefault();
                    if (eo != null)
                    {
                        SKImage image = ((Texture2D)eo).GetImage();
                        if (image != null)
                        {
                            return image.Encode().AsStream();
                        }
                    }
                }
            }

            return null;
        }

        public static void ExportAssetData(string fPath = null)
        {
            string fullPath = fPath == null ? TreeViewUtility.GetFullPath(FWindow.TVItem) + "/" + FWindow.FCurrentAsset : fPath;
            PakReader.PakReader reader = GetPakReader(fullPath);
            if (reader != null)
            {
                List<FPakEntry> entriesList = GetPakEntries(fullPath);
                foreach (FPakEntry entry in entriesList)
                {
                    string path = FProp.Default.FOutput_Path + "\\Exports\\" + entry.Name;
                    string pWExt = FoldersUtility.GetFullPathWithoutExtension(entry.Name);
                    string subfolders = pWExt.Substring(0, pWExt.LastIndexOf("/", StringComparison.InvariantCultureIgnoreCase));

                    Directory.CreateDirectory(FProp.Default.FOutput_Path + "\\Exports\\" + subfolders);
                    Stream stream = reader.GetPackageStream(entry);
                    using (var fStream = File.OpenWrite(path))
                    using (stream)
                    {
                        stream.CopyTo(fStream);
                    }

                    if (File.Exists(path))
                    {
                        new UpdateMyConsole(Path.GetFileName(path), CColors.Blue).Append();
                        new UpdateMyConsole(" successfully exported", CColors.White, true).Append();
                    }
                    else //just in case
                    {
                        new UpdateMyConsole("Bruh moment\nCouldn't export ", CColors.White).Append();
                        new UpdateMyConsole(Path.GetFileName(path), CColors.Blue, true).Append();
                    }
                }
            }
        }

        public static void SaveAssetProperties()
        {
            string prop = FWindow.FMain.AssetPropertiesBox_Main.Text;
            string path = FProp.Default.FOutput_Path + "\\JSONs\\" + FWindow.FCurrentAsset + ".json";
            if (!string.IsNullOrEmpty(prop))
            {
                File.WriteAllText(path, prop);
                if (File.Exists(path))
                {
                    new UpdateMyConsole(FWindow.FCurrentAsset, CColors.Blue).Append();
                    new UpdateMyConsole("'s Json data successfully saved", CColors.White, true).Append();
                }
                else //just in case
                {
                    new UpdateMyConsole("Bruh moment\nCouldn't export ", CColors.White).Append();
                    new UpdateMyConsole(FWindow.FCurrentAsset, CColors.Blue, true).Append();
                }
            }
        }

        public static bool IsValidJson(string strInput)
        {
            strInput = strInput.Trim();
            if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || //For object
                (strInput.StartsWith("[") && strInput.EndsWith("]"))) //For array
            {
                try
                {
                    JToken obj = JToken.Parse(strInput);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public static T GetPropertyTag<T>(JArray properties, string name)
        {
            return properties
                .Where(x => string.Equals(x["name"].Value<string>(), name))
                .Select(x => x["tag_data"].Value<T>())
                .FirstOrDefault();
        }
        public static T GetPropertyTagImport<T>(JArray properties, string name)
        {
            return properties
                .Where(x => string.Equals(x["name"].Value<string>(), name))
                .Select(x => x["tag_data"]["import"].Value<T>())
                .FirstOrDefault();
        }
        public static T GetPropertyTagOuterImport<T>(JArray properties, string name)
        {
            return properties
                .Where(x => string.Equals(x["name"].Value<string>(), name))
                .Select(x => x["tag_data"]["outer_import"].Value<T>())
                .FirstOrDefault();
        }
        public static T GetPropertyTagText<T>(JArray properties, string name, string tag_data)
        {
            return properties
                .Where(x => string.Equals(x["name"].Value<string>(), name))
                .Select(x => x["tag_data"][tag_data].Value<T>())
                .FirstOrDefault();
        }
        public static T GetPropertyTagStruct<T>(JArray properties, string name, string struct_type)
        {
            return properties
                .Where(x => string.Equals(x["name"].Value<string>(), name))
                .Select(x => x["tag_data"]["struct_type"][struct_type].Value<T>())
                .FirstOrDefault();
        }
        public static T GetPropertyTagItemData<T>(JArray properties, string item, string item_data)
        {
            return properties
                .Where(x => string.Equals(x["Item1"].Value<string>(), item, StringComparison.InvariantCultureIgnoreCase))
                .Select(x => x["Item2"][item_data].Value<T>())
                .FirstOrDefault();
        }
    }
}
