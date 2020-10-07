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
        public SKBitmap RarityBackgroundImage1;
        public SKBitmap RarityBackgroundImage2;
        public string RarityDisplayName;
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
            RarityBackgroundImage1 = null;
            RarityBackgroundImage2 = null;
            RarityDisplayName = "";
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
            EnumProperty r = export.GetExport<EnumProperty>("Rarity");
            Rarity.GetInGameRarity(this, r);
            this.RarityDisplayName = r != null ? r?.Value.String["EXRarity::".Length..] : "Common";

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

            RarityBackgroundImage1 = Utils.GetTexture("/Game/UI/Textures/assets/HUDAccentFillBox.HUDAccentFillBox");
            RarityBackgroundImage2 = Utils.GetTexture("/Game/UI/Textures/assets/store/ItemBGStatic_UIT.ItemBGStatic_UIT");
        }

        public void Draw(SKCanvas c)
        {
            if (this.RarityBackgroundImage1 != null)
                c.DrawBitmap(this.RarityBackgroundImage1, new SKRect(0, 0, Width, Height),
                    new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true });

            if (this.RarityBackgroundImage2 != null)
                c.DrawBitmap(this.RarityBackgroundImage2, new SKRect(0, 0, Width, Height),
                    new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true, Color = SKColors.Transparent.WithAlpha(75) });

            int x = this.Margin * (int)2.5;
            int radi = 15;
            c.DrawCircle(x + radi, x + radi, radi, new SKPaint
            {
                IsAntialias = true,
                FilterQuality = SKFilterQuality.High,
                Shader = SKShader.CreateRadialGradient(
                    new SKPoint(radi, radi),
                    (radi * 2) / 5 * 4,
                    this.RarityBackgroundColors,
                    SKShaderTileMode.Clamp)
            });
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
