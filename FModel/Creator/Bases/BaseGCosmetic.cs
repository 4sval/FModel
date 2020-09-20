using FModel.Creator.Rarities;
using FModel.Creator.Texts;
using PakReader.Parsers.Class;
using PakReader.Parsers.PropertyTagData;
using SkiaSharp;
using System;
using System.Windows;

namespace FModel.Creator.Bases
{
    public class BaseGCosmetic : IBase
    {
        public SKBitmap FallbackImage;
        public SKBitmap IconImage;
        public SKColor[] RarityBackgroundColors;
        public SKColor[] RarityBorderColor;
        public string DisplayName;
        public string Description;
        public int Width = 512; // keep it 512 (or a multiple of 512) if you don't want blurry icons
        public int Height = 512;
        public int Margin = 2;

        public BaseGCosmetic(string exportType)
        {
            FallbackImage = SKBitmap.Decode(Application.GetResourceStream(new Uri("pack://application:,,,/Resources/T_Placeholder_Item_Image.png")).Stream);
            IconImage = FallbackImage;
            RarityBackgroundColors = new SKColor[2] { SKColor.Parse("FFFFFF"), SKColor.Parse("636363") };
            RarityBorderColor = new SKColor[2] { SKColor.Parse("D0D0D0"), SKColor.Parse("FFFFFF") };
            DisplayName = "";
            Description = "";
            Width = exportType switch
            {
                "GCosmeticCard" => 1024,
                _ => 512
            };
            Height = exportType switch
            {
                "GCosmeticCard" => 200,
                _ => 512
            };
        }

        public BaseGCosmetic(IUExport export, string exportType) : this(exportType)
        {
            // rarity
            Rarity.GetInGameRarity(this, export.GetExport<EnumProperty>("Rarity"));

            // image
            if (export.GetExport<SoftObjectProperty>("IconTexture") is SoftObjectProperty previewImage)
                this.IconImage = Utils.GetSoftObjectTexture(previewImage);
            else if (export.GetExport<ObjectProperty>("IconTexture") is ObjectProperty iconTexture)
                this.IconImage = Utils.GetObjectTexture(iconTexture);

            // text
            if (export.GetExport<TextProperty>("DisplayName", "Title") is TextProperty displayName)
                DisplayName = Text.GetTextPropertyBase(displayName);
            if (export.GetExport<TextProperty>("Description") is TextProperty description)
                Description = Text.GetTextPropertyBase(description);
        }

        SKBitmap IBase.FallbackImage => FallbackImage;
        SKBitmap IBase.IconImage => IconImage;
        SKColor[] IBase.RarityBackgroundColors => RarityBackgroundColors;
        SKColor[] IBase.RarityBorderColor => RarityBorderColor;
        string IBase.DisplayName => DisplayName;
        string IBase.Description => Description;
        int IBase.Width => Width;
        int IBase.Height => Height;
        int IBase.Margin => Margin;
    }
}
