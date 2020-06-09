using PakReader.Parsers.Class;
using PakReader.Parsers.PropertyTagData;
using SkiaSharp;
using System;
using System.Windows;

namespace FModel.Creator
{
    public class BaseValorant
    {
        public SKBitmap FallbackImage;
        public SKBitmap IconImage;
        public string DisplayName;
        public string Description;
        public int Size = 512; // keep it 512 (or a multiple of 512) if you don't want blurry icons

        public BaseValorant()
        {
            FallbackImage = SKBitmap.Decode(Application.GetResourceStream(new Uri("pack://application:,,,/Resources/T_Placeholder_Item_Image.png")).Stream);
            IconImage = FallbackImage;
            DisplayName = "";
            Description = "";
        }

        public BaseValorant(IUExport export, string assetFolder, ref string assetName) : this()
        {
            //if (export.GetExport<ArrayProperty>("ExplicitAssets") is ArrayProperty explicitAssets)
        }
    }
}
