using FModel.Creator.Icons;
using FModel.Creator.Texts;
using FModel.Creator.Valorant;
using FModel.ViewModels.ImageBox;
using PakReader.Parsers.Class;
using SkiaSharp;
using System.IO;

namespace FModel.Creator
{
    static class ValorantCreator
    {
        public static bool TryDrawValorantIcon(string assetPath, string exportType, IUExport export)
        {
            var d = new DirectoryInfo(assetPath);
            string assetName = d.Name;
            if (Text.TypeFaces.NeedReload(false))
                Text.TypeFaces = new Typefaces();

            switch (exportType)
            {
                case "MapUIData":
                    {
                        BaseMapUIData icon = new BaseMapUIData(export);
                        using (var ret = new SKBitmap(icon.Width, icon.Height, SKColorType.Rgba8888, SKAlphaType.Premul))
                        using (var c = new SKCanvas(ret))
                        {
                            icon.Draw(c);
                            ImageBoxVm.imageBoxViewModel.Set(ret, assetName);
                        }
                        return true;
                    }
                case "ArmorUIData":
                case "SprayUIData":
                case "ThemeUIData":
                case "ContractUIData":
                case "CurrencyUIData":
                case "GameModeUIData":
                case "CharacterUIData":
                case "SprayLevelUIData":
                case "EquippableUIData":
                case "PlayerCardUIData":
                case "Gun_UIData_Base_C":
                case "CharacterRoleUIData":
                case "EquippableSkinUIData":
                case "EquippableCharmUIData":
                case "EquippableSkinLevelUIData":
                case "EquippableSkinChromaUIData":
                case "EquippableCharmLevelUIData":
                    {
                        BaseUIData icon = new BaseUIData(export);
                        using (var ret = new SKBitmap(icon.Width, icon.Height, SKColorType.Rgba8888, SKAlphaType.Premul))
                        using (var c = new SKCanvas(ret))
                        {
                            icon.Draw(c);

                            Watermark.DrawWatermark(c); // watermark should only be applied on icons with width = 512
                            ImageBoxVm.imageBoxViewModel.Set(ret, assetName);
                        }
                        return true;
                    }
            }
            return false;
        }
    }
}
