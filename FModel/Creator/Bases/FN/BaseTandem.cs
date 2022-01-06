using System;
using System.Windows;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using FModel.Framework;
using FModel.Settings;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace FModel.Creator.Bases.FN
{
    public class BaseTandem : BaseIcon
    {
        private string _generalDescription, _additionalDescription;

        public BaseTandem(UObject uObject, EIconStyle style) : base(uObject, style)
        {
            DefaultPreview = Utils.GetBitmap("FortniteGame/Content/UI/Foundation/Textures/BattleRoyale/FeaturedItems/Outfit/T-AthenaSoldiers-CID-883-Athena-Commando-M-ChOneJonesy.T-AthenaSoldiers-CID-883-Athena-Commando-M-ChOneJonesy");
            Margin = 0;
            Width = 690;
            Height = 1080;
        }

        public override void ParseForInfo()
        {
            base.ParseForInfo();

            string sidePanel = string.Empty, entryList = string.Empty;

            if (Object.TryGetValue(out FSoftObjectPath sidePanelIcon, "SidePanelIcon"))
                sidePanel = sidePanelIcon.AssetPathName.Text;
            if (Object.TryGetValue(out FSoftObjectPath entryListIcon, "EntryListIcon"))
                entryList = entryListIcon.AssetPathName.Text;

            // Overrides for generic "default" images Epic uses for Quest-only or unfinished NPCs
            if (sidePanel.Contains("Clown") && entryList.Contains("Clown"))
                Preview = null;
            else if (sidePanel.Contains("Bane") && !Object.Name.Contains("Sorana"))
                Preview = Utils.GetBitmap(entryList);
            else if (!string.IsNullOrWhiteSpace(sidePanel) && !sidePanel.Contains("Clown"))
                Preview = Utils.GetBitmap(sidePanel);
            else if ((string.IsNullOrWhiteSpace(sidePanel) || sidePanel.Contains("Clown")) && !string.IsNullOrWhiteSpace(entryList))
                Preview = Utils.GetBitmap(entryList);

            if (Object.TryGetValue(out FText genDesc, "GeneralDescription"))
                _generalDescription = genDesc.Text;
            if (Object.TryGetValue(out FText addDesc, "AdditionalDescription"))
                _additionalDescription = addDesc.Text;
        }

        public override SKImage Draw()
        {
            using var ret = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Opaque);
            using var c = new SKCanvas(ret);

            DrawBackground(c);
            DrawPreview(c);
            DrawHaze(c);

            // Korean is slightly smaller than other languages, so the font size is increased slightly
            DrawName(c);
            DrawGeneralDescription(c);
            DrawAdditionalDescription(c);

            return SKImage.FromBitmap(ret);
        }

        private readonly SKPaint _panelPaint = new() { IsAntialias = true, FilterQuality = SKFilterQuality.High, Color = SKColor.Parse("#0045C7") };

        private new void DrawBackground(SKCanvas c)
        {
            c.DrawBitmap(SKBitmap.Decode(Application.GetResourceStream(new Uri("pack://application:,,,/Resources/npcleftside.png"))?.Stream).Resize(Width, Height), 0, 0, new SKPaint { IsAntialias = false, FilterQuality = SKFilterQuality.None, ImageFilter = SKImageFilter.CreateBlur(0, 25) });

            using var rect1 = new SKPath { FillType = SKPathFillType.EvenOdd };
            _panelPaint.Color = SKColor.Parse("#002A8C");
            rect1.MoveTo(29, 0);
            rect1.LineTo(62, Height);
            rect1.LineTo(Width, Height);
            rect1.LineTo(Width, 0);
            rect1.LineTo(29, 0);
            rect1.Close();
            c.DrawPath(rect1, _panelPaint);

            _panelPaint.Shader = SKShader.CreateLinearGradient(new SKPoint(29, 0), new SKPoint(Width, Height),
                new[] { SKColor.Parse("#002A8C") }, SKShaderTileMode.Clamp);
            c.DrawPath(rect1, _panelPaint);

            _panelPaint.Shader = SKShader.CreateRadialGradient(new SKPoint(348, 196), 300, new[] { SKColor.Parse("#0049CE"), SKColor.Parse("#002A8C") }, SKShaderTileMode.Clamp);
            c.DrawPath(rect1, _panelPaint);

            using var rect2 = new SKPath { FillType = SKPathFillType.EvenOdd };

            rect2.MoveTo(10, 0);
            rect2.LineTo(30, 0);
            rect2.LineTo(63, Height);
            rect2.LineTo(56, Height);
            rect2.LineTo(10, 0);
            rect2.Close();
            c.DrawPath(rect2, _panelPaint);

            _panelPaint.Shader = SKShader.CreateLinearGradient(new SKPoint(10, 0), new SKPoint(62, Height),
                new[] { SKColor.Parse("#0045C7") }, SKShaderTileMode.Clamp);
            c.DrawPath(rect2, _panelPaint);
        }

        private new void DrawPreview(SKCanvas c)
        {
            var previewToUse = Preview ?? DefaultPreview;

            if (Preview == null)
            {
                previewToUse = DefaultPreview;
                ImagePaint.BlendMode = SKBlendMode.DstOut;
                ImagePaint.Color = SKColor.Parse("#00175F");
            }

            var x = -125;

            switch (previewToUse.Width)
            {
                case 512 when previewToUse.Height == 1024:
                    previewToUse = previewToUse.ResizeWithRatio(500, 1000);
                    x = 100;
                    break;
                case 512 when previewToUse.Height == 512:
                    previewToUse = previewToUse.Resize(512);
                    x = 125;
                    break;
                default:
                    previewToUse = previewToUse.Resize(1000, 1000);
                    break;
            }

            c.DrawBitmap(previewToUse, x, 30, ImagePaint);
        }

        private void DrawHaze(SKCanvas c)
        {
            using var rect1 = new SKPath { FillType = SKPathFillType.EvenOdd };
            rect1.MoveTo(29, 0);
            rect1.LineTo(62, Height);
            rect1.LineTo(Width, Height);
            rect1.LineTo(Width, 0);
            rect1.LineTo(29, 0);
            rect1.Close();

            _panelPaint.Shader = SKShader.CreateLinearGradient(new SKPoint(343, 0), new SKPoint(343, Height),
                new[] { SKColors.Transparent, SKColor.Parse("#001E70FF"), SKColor.Parse("#001E70").WithAlpha(200), SKColor.Parse("#001E70").WithAlpha(245), SKColor.Parse("#001E70") }, new[] { 0, (float) .1, (float) .65, (float) .85, 1 }, SKShaderTileMode.Clamp);
            c.DrawPath(rect1, _panelPaint);
        }

        private void DrawName(SKCanvas c)
        {
            if (string.IsNullOrWhiteSpace(DisplayName)) return;

            DisplayNamePaint.TextSize = UserSettings.Default.AssetLanguage switch
            {
                ELanguage.Korean => 56,
                _ => 42
            };

            DisplayNamePaint.TextScaleX = (float) 1.1;
            DisplayNamePaint.Color = SKColors.White;
            DisplayNamePaint.TextSkewX = (float) -.25;
            DisplayNamePaint.TextAlign = SKTextAlign.Left;

            var typeface = Utils.Typefaces.TandemDisplayName;
            if (typeface == Utils.Typefaces.Default)
            {
                DisplayNamePaint.TextSize = 30;
            }

            DisplayNamePaint.Typeface = typeface;
            var shaper = new CustomSKShaper(DisplayNamePaint.Typeface);
            c.DrawShapedText(shaper, DisplayName.ToUpper(), 97, 900, DisplayNamePaint);
        }

        private void DrawGeneralDescription(SKCanvas c)
        {
            if (string.IsNullOrWhiteSpace(_generalDescription)) return;

            DescriptionPaint.TextSize = UserSettings.Default.AssetLanguage switch
            {
                ELanguage.Korean => 20,
                _ => 17
            };

            DescriptionPaint.Color = SKColor.Parse("#00FFFB");
            DescriptionPaint.TextAlign = SKTextAlign.Left;

            var typeface = Utils.Typefaces.TandemGenDescription;
            if (typeface == Utils.Typefaces.Default)
            {
                DescriptionPaint.TextSize = 21;
            }

            DescriptionPaint.Typeface = typeface;
            var shaper = new CustomSKShaper(DescriptionPaint.Typeface);
            c.DrawShapedText(shaper, _generalDescription.ToUpper(), 97, 930, DescriptionPaint);
        }

        private void DrawAdditionalDescription(SKCanvas c)
        {
            if (string.IsNullOrWhiteSpace(_additionalDescription)) return;

            DescriptionPaint.TextSize = UserSettings.Default.AssetLanguage switch
            {
                ELanguage.Korean => 22,
                _ => 18
            };

            DescriptionPaint.Color = SKColor.Parse("#89D8FF");
            DescriptionPaint.TextAlign = SKTextAlign.Left;

            var typeface = Utils.Typefaces.TandemAddDescription;
            if (typeface == Utils.Typefaces.Default)
            {
                DescriptionPaint.TextSize = 20;
            }

            DescriptionPaint.Typeface = typeface;
            Utils.DrawMultilineText(c, _additionalDescription, Width, 0, SKTextAlign.Left,
                new SKRect(97, 960, Width - 10, Height), DescriptionPaint, out _);
        }
    }
}
