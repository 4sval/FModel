using FModel.Methods.SyntaxHighlighter;
using FModel.Methods.Utilities;
using Newtonsoft.Json;
using PakReader;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FModel.Methods.Assets
{
    class AssetsLoader
    {
        public static async void LoadSelectedAsset()
        {
            FWindow.FMain.AssetPropertiesBox_Main.Text = string.Empty;
            FWindow.FMain.AssetPropertiesBox_Main.SyntaxHighlighting = ResourceLoader.LoadHighlightingDefinition("Json.xshd");
            FWindow.FMain.ImageBox_Main.Source = null;

            await Task.Run(() =>
            {
                LoadAsset();
            }).ContinueWith(TheTask =>
            {
                TasksUtility.TaskCompleted(TheTask.Exception);
            });
        }

        private static void LoadAsset()
        {
            PakReader.PakReader reader = AssetsUtility.GetPakReader();
            if (reader != null)
            {
                IEnumerable<FPakEntry> entriesList = AssetsUtility.GetPakEntries(reader);
                Stream[] AssetStreamArray = new Stream[3];
                string jsonData = string.Empty;

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
                                jsonData = r.ReadToEnd();
                            break;
                        case ".uproject":
                        case ".uplugin":
                        case ".upluginmanifest":
                            using (var s = reader.GetPackageStream(entry))
                            using (var r = new StreamReader(s))
                                jsonData = r.ReadToEnd();
                            break;
                        case ".locmeta":
                            using (var s = reader.GetPackageStream(entry))
                                jsonData = JsonConvert.SerializeObject(new LocMetaFile(s), Formatting.Indented);
                            break;
                        case ".locres":
                            using (var s = reader.GetPackageStream(entry))
                                jsonData = JsonConvert.SerializeObject(new LocResFile(s).Entries, Formatting.Indented);
                            break;
                        case ".udic":
                            using (var s = reader.GetPackageStream(entry))
                            using (var r = new BinaryReader(s))
                                jsonData = JsonConvert.SerializeObject(new UDicFile(r).Header, Formatting.Indented);
                            break;
                        case ".bin":
                            if (string.Equals(entry.Name, "/FortniteGame/AssetRegistry.bin") || !entry.Name.Contains("AssetRegistry")) //MEMORY ISSUE
                                break;

                            using (var s = reader.GetPackageStream(entry))
                                jsonData = JsonConvert.SerializeObject(new AssetRegistryFile(s), Formatting.Indented);
                            break;
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

                AssetReader ar = AssetsUtility.GetAssetReader(AssetStreamArray);
                if (ar != null)
                {
                    jsonData = JsonConvert.SerializeObject(ar.Exports, Formatting.Indented);

                    ExportObject eo = ar.Exports.Where(x => x is Texture2D).FirstOrDefault();
                    if (eo != null)
                    {
                        FWindow.FMain.Dispatcher.InvokeAsync(() =>
                        {
                            SkiaSharp.SKImage image = ((Texture2D)eo).GetImage();
                            if (image != null)
                            {
                                using (var data = image.Encode())
                                using (var stream = data.AsStream())
                                {
                                    FWindow.FMain.ImageBox_Main.Source = ImagesUtility.GetImageSource(stream);
                                }
                            }
                        });
                    }
                }

                FWindow.FMain.Dispatcher.InvokeAsync(() =>
                {
                    FWindow.FMain.AssetPropertiesBox_Main.Text = jsonData;
                });
            }
        }
    }
}
