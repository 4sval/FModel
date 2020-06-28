using FModel.Creator.Icons;
using FModel.Creator.Texts;
using FModel.ViewModels.ImageBox;
using PakReader.Parsers.Class;
using SkiaSharp;
using System.IO;

namespace FModel.Creator
{
    static class ValorantCreator
    {
        public static bool TryDrawValorantIcon(string assetPath, IUExport export)
        {
            var d = new DirectoryInfo(assetPath);
            string assetName = d.Name;
            if (Text.TypeFaces.NeedReload(false))
                Text.TypeFaces = new Typefaces(); // when opening bundle creator settings without loading paks first

            BaseUIData icon = new BaseUIData(export);
            if (icon.IconImage != null)
            {
                using (var ret = new SKBitmap(icon.Width, icon.Height, SKColorType.Rgba8888, SKAlphaType.Premul))
                using (var c = new SKCanvas(ret))
                {
                    icon.Draw(c);

                    Watermark.DrawWatermark(c); // watermark should only be applied on icons with width = 512
                    ImageBoxVm.imageBoxViewModel.Set(ret, assetName);
                }
                return true;
            }

            return false;
        }
    }
}
