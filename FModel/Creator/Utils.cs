using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using CUE4Parse_Conversion.Textures;
using FModel.Framework;
using FModel.Services;
using FModel.ViewModels;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace FModel.Creator
{
    public static class Utils
    {
        private static ApplicationViewModel _applicationView => ApplicationService.ApplicationView;
        private static readonly Regex _htmlRegex = new("<.*?>");
        public static Typefaces Typefaces;

        public static string RemoveHtmlTags(string s)
        {
            var match = _htmlRegex.Match(s);
            while (match.Success)
            {
                s = s.Replace(match.Value, string.Empty);
                match = match.NextMatch();
            }

            return s;
        }

        public static bool TryGetDisplayAsset(UObject uObject, out SKBitmap preview)
        {
            if (uObject.TryGetValue(out FSoftObjectPath sidePanelIcon, "SidePanelIcon"))
            {
                preview = GetBitmap(sidePanelIcon);
                return preview != null;
            }

            var path = $"/Game/Catalog/MI_OfferImages/MI_{uObject.Name.Replace("Athena_Commando_", string.Empty)}";
            if (!TryLoadObject(path, out UMaterialInstanceConstant material)) // non-obfuscated item definition
            {
                if (!TryLoadObject($"{path[..path.LastIndexOf('_')]}_{path.SubstringAfterLast('_').ToLower()}", out material)) // Try to get MI with lowercase obfuscation 
                    TryLoadObject(path[..path.LastIndexOf('_')], out material); // hopefully gets obfuscated item definition
            }

            preview = GetBitmap(material);
            return preview != null;
        }

        public static SKBitmap GetBitmap(FPackageIndex packageIndex)
        {
            while (true)
            {
                if (!TryGetPackageIndexExport(packageIndex, out UObject export)) return null;
                switch (export)
                {
                    case UTexture2D texture:
                        return GetBitmap(texture);
                    case UMaterialInstanceConstant material:
                        return GetBitmap(material);
                    default:
                    {
                        if (export.TryGetValue(out FSoftObjectPath previewImage, "LargePreviewImage", "SmallPreviewImage")) return GetBitmap(previewImage);
                        if (export.TryGetValue(out string largePreview, "LargePreviewImage")) return GetBitmap(largePreview);
                        if (export.TryGetValue(out FPackageIndex smallPreview, "SmallPreviewImage"))
                        {
                            packageIndex = smallPreview;
                            continue;
                        }

                        return null;
                    }
                }
            }
        }
        public static SKBitmap GetBitmap(UMaterialInstanceConstant material)
        {
            if (material == null) return null;
            foreach (var textureParameter in material.TextureParameterValues)
            {
                if (!textureParameter.ParameterValue.TryLoad<UTexture2D>(out var texture)) continue;
                switch (textureParameter.ParameterInfo.Name.Text)
                {
                    case "MainTex":
                    case "TextureA":
                    case "TextureB":
                    case "OfferImage":
                    case "KeyArtTexture":
                    {
                        return GetBitmap(texture);
                    }
                }
            }

            return null;
        }
        public static SKBitmap GetB64Bitmap(string b64) => SKBitmap.Decode(new MemoryStream(Convert.FromBase64String(b64)) {Position = 0});
        public static SKBitmap GetBitmap(FSoftObjectPath softObjectPath) => GetBitmap(softObjectPath.AssetPathName.Text);
        public static SKBitmap GetBitmap(string fullPath) => TryLoadObject(fullPath, out UTexture2D texture) ? GetBitmap(texture) : null;
        public static SKBitmap GetBitmap(UTexture2D texture) => texture.IsVirtual ? null : SKBitmap.Decode(texture.Decode()?.Encode());
        public static SKBitmap GetBitmap(byte[] data) => SKBitmap.Decode(data);

        public static SKBitmap Resize(this SKBitmap me, int size) => me.Resize(size, size);

        public static SKBitmap Resize(this SKBitmap me, int width, int height)
        {
            var bmp = new SKBitmap(new SKImageInfo(width, height), SKBitmapAllocFlags.ZeroPixels);
            using var pixmap = bmp.PeekPixels();
            me.ScalePixels(pixmap, SKFilterQuality.Medium);
            return bmp;
        }

        public static bool TryGetPackageIndexExport<T>(FPackageIndex packageIndex, out T export) where T : UObject
        {
            if (packageIndex.ResolvedObject == null)
            {
                export = default;
                return false;
            }

            var outerChain = new List<string>();
            var current = packageIndex.ResolvedObject.Outer;
            while (current != null)
            {
                outerChain.Add(current.Name.Text);
                current = current.Outer;
            }

            if (outerChain.Count < 1)
            {
                export = default;
                return false;
            }

            if (!_applicationView.CUE4Parse.Provider.TryLoadPackage(outerChain[^1], out var pkg))
            {
                export = default;
                return false;
            }

            export = pkg.GetExport(packageIndex.ResolvedObject.Index) as T;
            return export != null;
        }

        // fullpath must be either without any extension or with the export objectname
        public static bool TryLoadObject<T>(string fullPath, out T export) where T : UObject
        {
            return _applicationView.CUE4Parse.Provider.TryLoadObject(fullPath, out export);
        }

        public static IEnumerable<UObject> LoadExports(string fullPath)
        {
            return _applicationView.CUE4Parse.Provider.LoadObjectExports(fullPath);
        }

        public static string GetLocalizedResource(string @namespace, string key, string defaultValue)
        {
            return _applicationView.CUE4Parse.Provider.GetLocalizedString(@namespace, key, defaultValue);
        }

        public static string GetFullPath(string partialPath)
        {
            var regex = new Regex(partialPath, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            foreach (var path in _applicationView.CUE4Parse.Provider.Files.Keys)
            {
                if (regex.IsMatch(path))
                {
                    return path;
                }
            }

            return string.Empty;
        }

        public static void DrawCenteredMultilineText(SKCanvas c, string text, int maxCount, int size, int margin, SKTextAlign side, SKRect area, SKPaint paint)
        {
            var lineHeight = paint.TextSize * 1.2f;
            var lines = SplitLines(text, paint, area.Width - margin);

#if DEBUG
            c.DrawRect(new SKRect(area.Left, area.Top, area.Right, area.Bottom), new SKPaint {Color = SKColors.Red, IsStroke = true});
#endif

            if (lines == null) return;
            if (lines.Count <= maxCount) maxCount = lines.Count;
            var height = maxCount * lineHeight;
            var y = area.MidY - height / 2;

            var shaper = new CustomSKShaper(paint.Typeface);
            for (var i = 0; i < maxCount; i++)
            {
                var line = lines[i];
                if (line == null) continue;

                var lineText = line.Trim();
                var shapedText = shaper.Shape(lineText, paint);

                y += lineHeight;
                var x = side switch
                {
                    SKTextAlign.Center => area.MidX - shapedText.Points[^1].X / 2,
                    SKTextAlign.Right => size - margin - shapedText.Points[^1].X,
                    SKTextAlign.Left => margin,
                    _ => throw new NotImplementedException()
                };

                c.DrawShapedText(shaper, lineText, x, y, paint);
            }
        }

        public static void DrawMultilineText(SKCanvas c, string text, int size, int margin, SKTextAlign side, SKRect area, SKPaint paint, out float yPos)
        {
            yPos = area.Top;
            var lineHeight = paint.TextSize * 1.2f;
            var lines = SplitLines(text, paint, area.Width);
            if (lines == null) return;

            foreach (var line in lines)
            {
                if (line == null) continue;
                var lineText = line.Trim();
                var shaper = new CustomSKShaper(paint.Typeface);
                var shapedText = shaper.Shape(lineText, paint);

                var x = side switch
                {
                    SKTextAlign.Center => area.MidX - shapedText.Points[^1].X / 2,
                    SKTextAlign.Right => size - margin - shapedText.Points[^1].X,
                    SKTextAlign.Left => area.Left,
                    _ => throw new NotImplementedException()
                };

                c.DrawShapedText(shaper, lineText, x, yPos, paint);
                yPos += lineHeight;
            }

#if DEBUG
            c.DrawRect(new SKRect(area.Left, area.Top - paint.TextSize, area.Right, yPos), new SKPaint {Color = SKColors.Red, IsStroke = true});
#endif
        }

        public static List<string> SplitLines(string text, SKPaint paint, float maxWidth)
        {
            if (string.IsNullOrEmpty(text)) return null;

            var spaceWidth = paint.MeasureText(" ");
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            var ret = new List<string>(lines.Length);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                float width = 0;
                var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var lineResult = new StringBuilder();
                foreach (var word in words)
                {
                    var wordWidth = paint.MeasureText(word);
                    var wordWithSpaceWidth = wordWidth + spaceWidth;
                    var wordWithSpace = word + " ";

                    if (width + wordWidth > maxWidth)
                    {
                        ret.Add(lineResult.ToString());
                        lineResult = new StringBuilder(wordWithSpace);
                        width = wordWithSpaceWidth;
                    }
                    else
                    {
                        lineResult.Append(wordWithSpace);
                        width += wordWithSpaceWidth;
                    }
                }

                ret.Add(lineResult.ToString());
            }

            return ret;
        }
    }
}