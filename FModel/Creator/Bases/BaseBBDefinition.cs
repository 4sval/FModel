using System;
using System.Windows;
using FModel.Creator.Rarities;
using FModel.Creator.Texts;
using FModel.PakReader.Parsers.Class;
using FModel.PakReader.Parsers.PropertyTagData;
using SkiaSharp;

namespace FModel.Creator.Bases
{
    public class BaseBBDefinition : IBase
    {
        public SKBitmap FallbackImage;
        public SKBitmap IconImage;
        public SKBitmap RarityBackgroundImage;
        public SKColor[] RarityBackgroundColors;
        public SKColor[] RarityBorderColor;
        public string DisplayName;
        public string Description;
        public int Width = 512;
        public int Height = 512;
        public int Margin = 2;

        public BaseBBDefinition(string exportType)
        {
            FallbackImage = SKBitmap.Decode(Application.GetResourceStream(new Uri("pack://application:,,,/Resources/T_Placeholder_Item_Image.png"))?.Stream);
            RarityBackgroundImage = null;
            IconImage = FallbackImage;
            RarityBackgroundColors = new[] { SKColor.Parse("D0D0D0"), SKColor.Parse("636363") };
            RarityBorderColor = new[] { SKColor.Parse("D0D0D0"), SKColor.Parse("FFFFFF") };
            DisplayName = "";
            Description = "";
        }

        public BaseBBDefinition(IUExport export, string exportType) : this(exportType)
        {
            if (export.GetExport<SoftObjectProperty>("IconTextureAssetData") is {} previewImage)
                IconImage = Utils.GetSoftObjectTexture(previewImage);
            else if (export.GetExport<ObjectProperty>("IconTextureAssetData") is {} iconTexture)
                IconImage = Utils.GetObjectTexture(iconTexture);
            
            if (export.GetExport<TextProperty>("DisplayName") is {} displayName)
                DisplayName = Text.GetTextPropertyBase(displayName);
            if (export.GetExport<TextProperty>("Description") is {} description)
                Description = Text.GetTextPropertyBase(description);
            
            RarityBackgroundImage = SKBitmap.Decode(Application.GetResourceStream(new Uri("pack://application:,,,/Resources/battle-breakers-item-background.png"))?.Stream);
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