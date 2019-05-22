using csharp_wick;
using FModel.Parser.RenderMat;
using FModel.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;

namespace FModel
{
    class JohnWick
    {
        public static PakAsset MyAsset;
        public static PakExtractor MyExtractor;

        private static string GetMountPointFromDict(string currentItem, bool DynamicPak = false)
        {
            if (DynamicPak == true)
                return ThePak.PaksMountPoint[ThePak.CurrentUsedPak ?? throw new InvalidOperationException()];
            else
                return ThePak.PaksMountPoint[ThePak.AllpaksDictionary[currentItem ?? throw new InvalidOperationException()] ?? throw new InvalidOperationException()];
        }
        private static string WriteFile(string currentItem, string myResults, byte[] data)
        {
            if (ThePak.CurrentUsedPakGuid != null && ThePak.CurrentUsedPakGuid != "0-0-0-0")
            {
                Directory.CreateDirectory(App.DefaultOutputPath + "\\Extracted\\" + GetMountPointFromDict(currentItem, true) + myResults.Substring(0, myResults.LastIndexOf("/", StringComparison.Ordinal)));
                File.WriteAllBytes(App.DefaultOutputPath + "\\Extracted\\" + GetMountPointFromDict(currentItem, true) + myResults, data);

                return App.DefaultOutputPath + "\\Extracted\\" + GetMountPointFromDict(currentItem, true) + myResults;
            }
            else
            {
                Directory.CreateDirectory(App.DefaultOutputPath + "\\Extracted\\" + GetMountPointFromDict(currentItem) + myResults.Substring(0, myResults.LastIndexOf("/", StringComparison.Ordinal)));
                File.WriteAllBytes(App.DefaultOutputPath + "\\Extracted\\" + GetMountPointFromDict(currentItem) + myResults, data);

                return App.DefaultOutputPath + "\\Extracted\\" + GetMountPointFromDict(currentItem) + myResults;
            }
        }

        public static string ExtractAsset(string currentPak, string currentItem)
        {
            MyExtractor = new PakExtractor(Settings.Default.PAKsPath + "\\" + currentPak, Settings.Default.AESKey);
            string[] myArray = MyExtractor.GetFileList().ToArray();

            string[] results;
            if (currentItem.Contains("."))
                results = Array.FindAll(myArray, s => s.Contains("/" + currentItem));
            else
                results = Array.FindAll(myArray, s => s.Contains("/" + currentItem + "."));

            string AssetPath = string.Empty;
            for (int i = 0; i < results.Length; i++)
            {
                int index = Array.IndexOf(myArray, results[i]);

                uint y = (uint)index;
                byte[] b = MyExtractor.GetData(y);

                AssetPath = WriteFile(currentItem, results[i], b).Replace("/", "\\");
            }
            return AssetPath;
        }
        public static string AssetToTexture2D(string AssetName)
        {
            string textureFilePath;
            if (ThePak.CurrentUsedPakGuid != null && ThePak.CurrentUsedPakGuid != "0-0-0-0")
                textureFilePath = ExtractAsset(ThePak.CurrentUsedPak, AssetName);
            else
                textureFilePath = ExtractAsset(ThePak.AllpaksDictionary[AssetName ?? throw new InvalidOperationException()], AssetName);


            string TexturePath = string.Empty;
            if (textureFilePath != null && (textureFilePath.Contains("MI_UI_FeaturedRenderSwitch_") || textureFilePath.Contains("M_UI_ChallengeTile_PCB") || textureFilePath.Contains("Wraps\\FeaturedMaterials\\")))
            {
                return GetRenderSwitchMaterialTexture(AssetName, textureFilePath);
            }
            else if (textureFilePath != null)
            {
                MyAsset = new PakAsset(textureFilePath.Substring(0, textureFilePath.LastIndexOf(".", StringComparison.Ordinal)));
                MyAsset.SaveTexture(textureFilePath.Substring(0, textureFilePath.LastIndexOf(".", StringComparison.Ordinal)) + ".png");
                TexturePath = textureFilePath.Substring(0, textureFilePath.LastIndexOf(".", StringComparison.Ordinal)) + ".png";
            }
            return TexturePath;
        }
        public static string GetRenderSwitchMaterialTexture(string AssetName, string AssetPath)
        {
            string TexturePath = string.Empty;
            if (AssetPath.Contains(".uasset") || AssetPath.Contains(".uexp") || AssetPath.Contains(".ubulk"))
            {
                MyAsset = new PakAsset(AssetPath.Substring(0, AssetPath.LastIndexOf('.')));
                try
                {
                    if (MyAsset.GetSerialized() != null)
                    {
                        string parsedRsmJson = JToken.Parse(MyAsset.GetSerialized()).ToString();
                        var rsmid = RenderSwitchMaterial.FromJson(parsedRsmJson);
                        for (int i = 0; i < rsmid.Length; i++)
                        {
                            if (rsmid[i].TextureParameterValues.FirstOrDefault()?.ParameterValue != null)
                            {
                                string textureFile = rsmid[i].TextureParameterValues.FirstOrDefault()?.ParameterValue;

                                TexturePath = AssetToTexture2D(textureFile);
                            }
                        }
                    }
                }
                catch (JsonSerializationException) { }
            }
            return TexturePath;
        }
    }
}
